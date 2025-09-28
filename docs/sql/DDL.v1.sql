---------------------------------------------------------
--------------             DDL             --------------
---------------------------------------------------------
/*
    File:        DDL.v1.sql
    Purpose:     Complete rebuild script for the ShelfMarket_Dev database (development only).
    Safety:      This script DROPS and RECREATES the database. Do NOT run in production.
    Conventions:
        - Naming: Tables use upper snake case to mirror existing physical conventions.
        - All temporal business columns use DATE where time-of-day is irrelevant.
        - All GUID PKs use NEWID() (not sequential) because write hotspot risk is minimal at dev scale.
        - INT IDENTITY used for human-facing incrementing numbers (e.g. ContractNumber).
        - Explicit nonclustered indexes created for frequent lookup & range predicates.
    Performance Considerations:
        - Composite index on SHELFTENANTCONTRACT supports tenant/date range & contract number seeks.
        - Covering index on SHELFTENANTCONTRACTLINE avoids key lookups for shelf resolution.
        - Shelf number index supports UI / ordering queries.
        - Email index supports unique tenant lookups.
    Change Log:
        2025-09-27  Added indexes section with rationale (IX_*).
        2025-09-27  Initial documentation comments added for all objects.
        2025-09-28  Added documentation blocks for TAXRATES, SALESRECEIPT, SALESRECEIPTLINE.
*/

-- Treats double quotes (") as identifier delimiters (object names), not as string delimiters.
SET QUOTED_IDENTIFIER ON;
GO

/***************************************************************************************************
  SECTION: Database Reset (Development Only)
  - Forces single user, drops db if exists, recreates a clean environment.
***************************************************************************************************/
USE master;
GO

IF DB_ID(N'ShelfMarket_Dev') IS NOT NULL
BEGIN
    PRINT 'Dropping existing ShelfMarket_Dev...';
    ALTER DATABASE [ShelfMarket_Dev] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [ShelfMarket_Dev];
END
GO

PRINT 'Creating database ShelfMarket_Dev...';
CREATE DATABASE [ShelfMarket_Dev];
GO

USE [ShelfMarket_Dev];
GO

/***************************************************************************************************
  TABLE: VERSIONINFO
  Purpose:
      Tracks applied schema/data version seeds for manual / lightweight migration traceability.
  Columns:
      Id          - Static key (e.g., 1) for initial seed row.
      Version     - Semantic version or descriptive label.
      AppliedOn   - Timestamp when record inserted (defaults to current date/time).
  Notes:
      Not intended to replace a formal migration system; acts as a checkpoint.
***************************************************************************************************/
CREATE TABLE [dbo].[VERSIONINFO] (
    [Id] INT NOT NULL PRIMARY KEY,
    [Version] NVARCHAR(50) NOT NULL,
    [AppliedOn] DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/***************************************************************************************************
  TABLE: COMPANYINFO
  Purpose:
      Stores high-level company / organizational information that governs global taxation behavior
      or identifies the operating entity (e.g., for invoice headers).
  Columns:
      Id               - PK (GUID).
      Name             - Company legal or display name.
      Address/PostalCode/City - Optional physical location data.
      Email / PhoneNumber     - Contact channels (not constrained unique here).
      IsTaxRegistered  - Indicates whether the company is registered for the applicable tax regime.
                         0 = Not registered (no tax should be applied).
                         1 = Registered (tax may be applied to qualifying items).
      IsTaxUsedItem    - Indicates whether taxable items usage / logic is enabled at the company level.
                         Must NOT be 1 when IsTaxRegistered = 0 (enforced by check constraint).
  Constraints:
      DF_CompanyInfo_IsTaxRegistered  - Default 0 (conservative; opt-in to tax registration).
      DF_CompanyInfo_IsTaxUsedItem    - Default 0 (no taxable item usage unless explicitly enabled).
      CK_CompanyInfo_TaxUsageConsistency:
            Ensures IsTaxUsedItem <= IsTaxRegistered
            Equivalent logic: (IsTaxRegistered = 1 OR IsTaxUsedItem = 0).
  Notes:
      Removed prior erroneous foreign key reference (there is no ShelfTenantContractId column here).
      Future:
        - Consider UNIQUE (Email) if one authoritative contact address is required.
        - Consider adding TaxNumber / VATNumber columns and effective dating for registration changes.
***************************************************************************************************/
CREATE TABLE [dbo].[COMPANYINFO] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(255) NOT NULL,
    [Address] NVARCHAR(255) NULL,
    [PostalCode] NVARCHAR(20) NULL,
    [City] NVARCHAR(100) NULL,
    [Email] NVARCHAR(255) NULL,
    [PhoneNumber] NVARCHAR(50) NULL,
    [IsTaxRegistered] BIT NOT NULL CONSTRAINT DF_CompanyInfo_IsTaxRegistered DEFAULT(0),
    [IsTaxUsedItem] BIT NOT NULL CONSTRAINT DF_CompanyInfo_IsTaxUsedItem DEFAULT(0),
    CONSTRAINT CK_CompanyInfo_TaxUsageConsistency CHECK ([IsTaxUsedItem] <= [IsTaxRegistered])
);
GO

/***************************************************************************************************
  TABLE: SHELFTYPE
  Purpose:
      Master data defining classification / physical or pricing grouping for shelves.
  Columns:
      Id          - PK (GUID).
      Name        - Unique logical name (enforced UNIQUE).
      Description - Optional explanatory text.
  Constraints:
      UNIQUE(Name) ensures no duplicates for type references.
***************************************************************************************************/
CREATE TABLE [dbo].[SHELFTYPE] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(255) NOT NULL UNIQUE,
    [Description] NVARCHAR(200) NULL
);
GO

/***************************************************************************************************
  TABLE: SHELF
  Purpose:
      Represents individual rentable physical shelves / units.
  Columns:
      Id                    - PK (GUID).
      Number                - Business-assigned shelf number (not enforced unique; may allow reuse).
      ShelfTypeId           - FK to SHELFTYPE.
      LocationX / LocationY - Grid / coordinate mapping for UI or physical layout.
      OrientationHorizontal - 1 = horizontal, 0 = vertical (layout rendering hint).
  Constraints:
      FK_SHELF_SHELFTYPE    - Cascades on delete (removes shelves when type removed).
      UNIQUE(LocationX, LocationY) - Prevents positional overlap.
  Index Strategy:
      Separate nonclustered index on Number (see below) to support ordered listings.
***************************************************************************************************/
CREATE TABLE [dbo].[SHELF] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Number] INT NOT NULL,
    [ShelfTypeId] UNIQUEIDENTIFIER NOT NULL,
    [LocationX] INT NOT NULL,
    [LocationY] INT NOT NULL,
    [OrientationHorizontal] BIT NOT NULL CONSTRAINT DF_SHELF_OrientationHorizontal DEFAULT(1),
    CONSTRAINT FK_SHELF_SHELFTYPE FOREIGN KEY (ShelfTypeId) REFERENCES [dbo].[SHELFTYPE](Id) ON DELETE CASCADE,
    UNIQUE ([LocationX], [LocationY])
);
GO

/***************************************************************************************************
  TABLE: SHELFTENANT
  Purpose:
      Stores tenant/customer metadata used for contract associations.
  Columns:
      Id, FirstName, LastName - Identity & contact.
      Address / PostalCode / City - Optional physical contact.
      Email - Optional but unique if present (business lookup).
      PhoneNumber - Optional contact channel.
      Status - Arbitrary state (e.g., Active, Suspended); no enforced domain table yet.
  Constraints:
      UQ_SHELF_TENANT_EMAIL enforces uniqueness of email when provided (NULLs allowed).
***************************************************************************************************/
CREATE TABLE [dbo].[SHELFTENANT] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [FirstName] NVARCHAR(255) NOT NULL,
    [LastName] NVARCHAR(255) NOT NULL,
    [Address] NVARCHAR(255) NULL,
    [PostalCode] NVARCHAR(20) NULL,
    [City] NVARCHAR(100) NULL,
    [Email] NVARCHAR(255) NULL,
    [PhoneNumber] NVARCHAR(50) NULL,
    [Status] NVARCHAR(50) NOT NULL,
    CONSTRAINT [UQ_SHELF_TENANT_EMAIL] UNIQUE ([Email])
);
GO

/***************************************************************************************************
  TABLE: SHELFTENANTCONTRACT
  Purpose:
      Captures contract periods for a tenant, including lifecycle (cancellation).
  Columns:
      Id             - PK.
      ShelfTenantId  - FK to SHELFTENANT (contracts are tenant-scoped).
      ContractNumber - Sequential IDENTITY (stable human reference).
      StartDate / EndDate - Planned contract period (inclusive).
      CancelledAt    - Early termination date (<= EndDate) if contract cancelled.
  Business Rules (Not enforced here):
      - StartDate <= EndDate.
      - CancelledAt must be between StartDate and EndDate (if provided).
  Index Strategy:
      Composite index added post creation covering tenant & date access patterns.
***************************************************************************************************/
CREATE TABLE [dbo].[SHELFTENANTCONTRACT] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ShelfTenantId] UNIQUEIDENTIFIER NOT NULL,
    [ContractNumber] INT IDENTITY(1, 1) NOT NULL UNIQUE,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NOT NULL,
    [CancelledAt] DATE NULL,
    CONSTRAINT [FK_ShelfTenant] FOREIGN KEY ([ShelfTenantId]) REFERENCES [dbo].[SHELFTENANT]([Id]) ON DELETE CASCADE
);
GO

/***************************************************************************************************
  TABLE: SHELFTENANTCONTRACTLINE
  Purpose:
      Associates specific shelves with a contract. Supports multi-shelf contracts.
  Columns:
      Id                    - PK.
      ShelfTenantContractId - FK to contract header.
      ShelfId               - Shelf being rented.
      LineNumber            - Logical ordering / line identity within contract (unique per contract).
      PricePerMonth         - Standard pricing value.
      PricePerMonthSpecial  - Optional negotiated override (NULL => use standard price).
  Notes:
      - No uniqueness constraint across (ShelfId, Contract period). Overlapping allocations must be
        handled in application logic if exclusivity is required.
      - Consider future constraint or exclusion logic if double-booking should be prevented.
  Index Strategy:
      See IX_ShelfTenantContractLine_Contract_Shelf covering join + projection needs.
***************************************************************************************************/
CREATE TABLE [dbo].[SHELFTENANTCONTRACTLINE] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ShelfTenantContractId] UNIQUEIDENTIFIER NOT NULL,
    [ShelfId] UNIQUEIDENTIFIER NOT NULL,
    [LineNumber] INT NOT NULL,
    [PricePerMonth] DECIMAL(18, 2) NOT NULL,
    [PricePerMonthSpecial] DECIMAL(18, 2),
    CONSTRAINT [UQ_ShelfTenantContract_LineNumber] UNIQUE ([ShelfTenantContractId], [LineNumber]),
    CONSTRAINT [FK_ShelfTenantContract] FOREIGN KEY ([ShelfTenantContractId]) REFERENCES [dbo].[SHELFTENANTCONTRACT]([Id]) ON DELETE CASCADE
);
GO

/***************************************************************************************************
  TABLE: SHELFPRICINGRULES
  Purpose:
      Tiered pricing rules based on shelf count thresholds (e.g., volume discount logic).
  Columns:
      Id                  - PK.
      MinShelvesInclusive - Threshold (inclusive) that activates this tier.
      PricePerShelf       - Applied unit price from this threshold until next tier.
  Constraints:
      Unique on MinShelvesInclusive to prevent duplicate tier definitions.
  Future:
      - Add EffectiveFrom / EffectiveTo for temporal pricing evolution.
      - Potential IsActive flag for soft retirement of tiers.
***************************************************************************************************/
CREATE TABLE [dbo].[SHELFPRICINGRULES] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [MinShelvesInclusive] INT NOT NULL,
    [PricePerShelf] DECIMAL(5,2) NOT NULL,
    CONSTRAINT [UQ_ShelfPricingRules_MinShelvesInclusive] UNIQUE ([MinShelvesInclusive])
);
GO

/***************************************************************************************************
  TABLE: TAXRATES
  Purpose:
      Stores named tax rate definitions with effective dating to allow historical reconstruction
      of tax calculations (e.g., for audit or reproducing historical receipts).
  Columns:
      Id            - PK (GUID).
      Name          - Logical tax identifier (e.g., 'VAT', 'ReducedVAT', 'ExemptCodeX').
      RatePercent   - Percentage expressed as whole percent (e.g., 25.00 for 25%).
      EffectiveFrom - First date (inclusive) this rate version is valid.
      EffectiveTo   - Last date (inclusive) this rate version is valid. NULL = still active / open ended.
  Constraints:
      UQ_TaxRates_Name_EffectiveFrom ensures no duplicate rate versions start on same date for a name.
  Usage:
      - Application should select the single row where Name = ? AND EffectiveFrom <= @UsageDate
        AND (EffectiveTo IS NULL OR EffectiveTo >= @UsageDate).
  Future:
      - Consider filtered unique index preventing overlapping periods per Name.
      - Consider adding Jurisdiction / CountryCode if multi-region.
***************************************************************************************************/
CREATE TABLE [dbo].[TAXRATES] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(255) NOT NULL,
    [RatePercent] DECIMAL(5,2) NOT NULL,
    [EffectiveFrom] DATE NOT NULL,
    [EffectiveTo] DATE NULL,
    CONSTRAINT [UQ_TaxRates_Name_EffectiveFrom] UNIQUE ([Name], [EffectiveFrom])
);
GO

/***************************************************************************************************
  TABLE: SALESRECEIPT
  Purpose:
      Header record for a sales transaction (e.g., rental charge group or point-of-sale operation).
  Columns:
      Id                   - PK (GUID).
      ReceiptNumber        - Human friendly sequential identifier (IDENTITY).
      IssuedAt             - Timestamp of issuance (defaults current date/time).
      TotalAmount          - Gross total including tax (or net? depends on domain decision).
      TaxAmount            - Tax component of the total (redundant; ensure consistency).
      PaidByCash           - Flag indicating cash settlement method used.
      PaidByMobile         - Flag indicating mobile payment method used (not mutually exclusive).
      (Missing) ShelfTenantContractId - Referenced by defined FK; column not presently declared.
  Constraints:
      FK_SalesReceipt_ShelfTenantContract - References SHELFTENANTCONTRACT (ON DELETE CASCADE).
        NOTE: The referenced column ShelfTenantContractId does not exist in this table and must
              be added (e.g., UNIQUEIDENTIFIER NULL/NOT NULL) for the FK to function.
  Data Integrity Considerations:
      - Either PaidByCash, PaidByMobile (or future methods) should have at least one = 1; can enforce
        via CHECK constraint later.
      - TotalAmount >= TaxAmount >= 0 expected (potential future CHECK).
  Future:
      - Add PaymentMethod normalization (separate table) or bitmask style.
      - Add NetAmount if required to disambiguate calculation logic.
      - Add CurrencyCode if multi-currency scenarios emerge.
***************************************************************************************************/
CREATE TABLE [dbo].[SALESRECEIPT] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ReceiptNumber] INT IDENTITY(1,1) NOT NULL UNIQUE,
    [IssuedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [TotalAmount] DECIMAL(18,2) NOT NULL,
    [TaxAmount] DECIMAL(18,2) NOT NULL,
    [PaidByCash] BIT NOT NULL CONSTRAINT DF_SalesReceipt_PaidByCash DEFAULT(0),
    [PaidByMobile] BIT NOT NULL CONSTRAINT DF_SalesReceipt_PaidByMobile DEFAULT(0),
);
GO

/***************************************************************************************************
  TABLE: SALESRECEIPTLINE
  Purpose:
      Line-level monetary components belonging to a SALESRECEIPT header.
  Columns:
      Id             - PK (GUID).
      SalesReceiptId - FK to SALESRECEIPT.
      UnitPrice      - Monetary value for the line (net or gross depending on domain rule).
  Constraints:
      FK_SalesReceipt_SalesReceiptLine - Cascades on delete of parent receipt.
  Notes:
      - Extend with Quantity, TaxRateId, Description for richer invoice detail.
      - Consider adding LineNumber for deterministic ordering.
      - If tax details vary per line, storing TaxAmount per line recommended.
  Index Strategy:
      IX_SalesReceiptLine_SalesReceipt (see below) enables efficient receipt -> lines retrieval.
***************************************************************************************************/
CREATE TABLE [dbo].[SALESRECEIPTLINE] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ShelfNumber] INT NOT NULL,
    [SalesReceiptId] UNIQUEIDENTIFIER NOT NULL,
    [UnitPrice] DECIMAL(18,2) NOT NULL,
    CONSTRAINT [FK_SalesReceipt_SalesReceiptLine] FOREIGN KEY ([SalesReceiptId]) REFERENCES [dbo].[SALESRECEIPT]([Id]) ON DELETE CASCADE
);
GO

/***************************************************************************************************
  SEED DATA
***************************************************************************************************/
INSERT [dbo].[VERSIONINFO] ([Id], [Version], [AppliedOn]) VALUES (1, 'Initial', CAST('2025-10-06' AS date));
GO

/***************************************************************************************************
  INDEXES
  Rationale Summary:
    IX_ShelfTenantContract_Tenant_Contract_Dates
        - Pattern: WHERE ShelfTenantId = ? AND StartDate/EndDate range logic (active windows).
        - ContractNumber included in key for rapid drill-down (ORDER / direct lookup).
        - CancelledAt INCLUDED to satisfy COALESCE predicates without key expansion.
    IX_ShelfTenantContractLine_Contract_Shelf
        - Supports joins from contract -> shelves and projection of pricing info without lookup.
    IX_Shelf_Number
        - Supports ordered shelf listing & numeric search (e.g., barcode generation reference).
    IX_ShelfTenant_Email
        - Rapid tenant lookup by email (login/admin screens).
    IX_SalesReceip_IssuedAt
        - Supports chronological listing / paging by issue date + secondary order by receipt number.
    IX_SalesReceiptLine_SalesReceipt
        - Optimizes retrieval of all lines for a receipt without scan.
***************************************************************************************************/
CREATE NONCLUSTERED INDEX IX_ShelfTenantContract_Tenant_Contract_Dates
    ON dbo.SHELFTENANTCONTRACT (ShelfTenantId, ContractNumber, StartDate, EndDate)
    INCLUDE (CancelledAt);
GO

CREATE NONCLUSTERED INDEX IX_ShelfTenantContractLine_Contract_Shelf
    ON dbo.SHELFTENANTCONTRACTLINE (ShelfTenantContractId, ShelfId)
    INCLUDE (LineNumber, PricePerMonth, PricePerMonthSpecial);
GO

CREATE NONCLUSTERED INDEX IX_Shelf_Number
    ON dbo.SHELF (Number);
GO

CREATE NONCLUSTERED INDEX IX_ShelfTenant_Email
    ON dbo.SHELFTENANT (Email);
GO

CREATE NONCLUSTERED INDEX IX_SalesReceip_IssuedAt
    ON dbo.SALESRECEIPT (IssuedAt, ReceiptNumber);
GO

CREATE NONCLUSTERED INDEX IX_SalesReceiptLine_SalesReceipt
    ON dbo.SALESRECEIPTLINE (SalesReceiptId);
GO