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
        2025-09-28  Added documentation blocks for TAXRATES (renamed physical table VATRATES), SALESRECEIPT, SALESRECEIPTLINE.
        2025-09-28  Added documentation block for COMMISSION.
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
      AppliedAt   - Timestamp when record inserted (defaults to current date/time).
  Notes:
      Not intended to replace a formal migration system; acts as a checkpoint.
***************************************************************************************************/
CREATE TABLE [dbo].[VERSIONINFO] (
    [Id] INT NOT NULL PRIMARY KEY,
    [Version] NVARCHAR(50) NOT NULL,
    [AppliedAt] DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/***************************************************************************************************
  TABLE: VATRATES   (Logical concept: Tax Rates)
  Purpose:
      Stores named tax (VAT) rate definitions with effective dating to allow historical reconstruction
      of tax calculations (audit / reproduction of historical receipts).
  Naming Note:
      Documentation earlier referred to TAXRATES. Physical table name here is VATRATES.
  Columns:
      Id            - PK (GUID).
      Name          - Logical tax code (e.g., 'VAT', 'ReducedVAT', 'ExemptCodeX').
      RatePercent   - Whole percent (25.00 represents 25%).
      EffectiveFrom - First valid date (inclusive).
      EffectiveTo   - Last valid date (inclusive). NULL = open ended.
  Constraints:
      UQ_TaxRates_Name_EffectiveFrom prevents duplicate concurrent starting definitions per Name.
  Usage Pattern:
      SELECT TOP(1) * FROM VATRATES
        WHERE Name = @Name
          AND EffectiveFrom <= @UsageDate
          AND (EffectiveTo IS NULL OR EffectiveTo >= @UsageDate)
        ORDER BY EffectiveFrom DESC;
  Future Enhancements:
      - Add exclusion logic (filtered unique index) to prevent overlapping effective periods.
      - Add Jurisdiction / Country / Currency columns for multi-region use.
***************************************************************************************************/
CREATE TABLE [dbo].[VATRATES] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(255) NOT NULL,
    [RatePercent] DECIMAL(5,2) NOT NULL,
    [EffectiveFrom] DATE NOT NULL,
    [EffectiveTo] DATE NULL,
    CONSTRAINT [UQ_TaxRates_Name_EffectiveFrom] UNIQUE ([Name], [EffectiveFrom])
);
GO

/***************************************************************************************************
  TABLE: COMPANYINFO
  Purpose:
      Stores high-level company / organizational profile and tax enablement flags.
  Columns:
      Id                 - PK.
      Name               - Legal/display name.
      Address/Postal/City- Optional location.
      Email/PhoneNumber  - Contact channels (not enforced unique).
      IsTaxRegistered    - 1 if registered for VAT regime (prerequisite for any tax application).
      IsTaxUsedItem      - 1 if item-level tax logic enabled. Must be 0 when IsTaxRegistered = 0.
  Constraints:
      CK_CompanyInfo_TaxUsageConsistency ensures IsTaxUsedItem <= IsTaxRegistered.
      Defaults conservatively disable tax features until explicitly turned on.
  Future:
      - Add VatNumber / RegistrationEffectiveFrom/To for regulatory audits.
      - Optional unique constraint on Email for canonical contact address.
***************************************************************************************************/
CREATE TABLE [dbo].[COMPANYINFO] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(255) NOT NULL,
    [Address] NVARCHAR(255) NULL,
    [PostalCode] NVARCHAR(20) NULL,
    [City] NVARCHAR(100) NULL,
    [Email] NVARCHAR(255) NULL,
    [PhoneNumber] NVARCHAR(50) NULL,
    [CvrNumber] NVARCHAR(50) NULL,
    [IsTaxRegistered] BIT NOT NULL CONSTRAINT DF_CompanyInfo_IsTaxRegistered DEFAULT(0),
    [IsTaxUsedItem] BIT NOT NULL CONSTRAINT DF_CompanyInfo_IsTaxUsedItem DEFAULT(0),
    CONSTRAINT CK_CompanyInfo_TaxUsageConsistency CHECK ([IsTaxUsedItem] <= [IsTaxRegistered])
);
GO

/***************************************************************************************************
  TABLE: COMMISSION
  Purpose:
      Defines commission rates (percentage-based) with effective dating to support historical
      calculation of commissionable amounts (e.g., sales settlement).
  Columns:
      Id            - PK (GUID).
      RateProcent   - Commission percentage stored as floating whole percent (e.g., 10 = 10%).
                      NOTE: Consider changing to DECIMAL(5,2) for financial precision in future.
      EffectiveFrom - First date (inclusive) the commission rate becomes active.
      EffectiveTo   - Last date (inclusive) the rate is valid. NULL = still active / open ended.
  Business Rules (not enforced here):
      - Periods for the same logical commission program should not overlap (application responsibility).
  Future:
      - Add Name/ProgramCode if multiple commission schemes are required.
      - Add CreatedBy / Audit columns for administrative traceability.
***************************************************************************************************/
CREATE TABLE [dbo].[COMMISSION] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [RateProcent] FLOAT NOT NULL,
    [EffectiveFrom] DATE NOT NULL,
    [EffectiveTo] DATE NULL
);
GO

/***************************************************************************************************
  TABLE: SHELFPRICINGRULES
  Purpose:
      Tiered pricing rules based on shelf count thresholds (volume discount logic).
  Columns:
      Id                  - PK.
      MinShelvesInclusive - Threshold (inclusive) activating this price tier.
      PricePerShelf       - Applied unit price for counts >= threshold until next tier threshold.
  Constraints:
      Unique on MinShelvesInclusive prevents duplicate thresholds.
  Notes:
      - Current design assumes non-overlapping, increasing thresholds (1,2,4,...).
  Future:
      - Add EffectiveFrom / EffectiveTo for temporal evolution of pricing.
      - Add IsActive or soft retirement mechanism.
***************************************************************************************************/
CREATE TABLE [dbo].[SHELFPRICINGRULES] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [MinShelvesInclusive] INT NOT NULL,
    [PricePerShelf] DECIMAL(5,2) NOT NULL,
    CONSTRAINT [UQ_ShelfPricingRules_MinShelvesInclusive] UNIQUE ([MinShelvesInclusive])
);
GO

/***************************************************************************************************
  TABLE: SHELFTYPE
  Purpose:
      Master data classifying shelves (physical configuration or marketing category).
  Columns:
      Id          - PK.
      Name        - Unique logical name.
      Description - Optional descriptive text.
  Constraints:
      UNIQUE(Name) ensures consistent referencing and prevents duplicate labels.
  Future:
      - Add Dimensions / Capacity metadata if needed for allocation algorithms.
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
      Represents a rentable physical storage/display shelf.
  Columns:
      Id                   - PK.
      Number               - Business-facing shelf number (not enforced unique to allow reuse).
      ShelfTypeId          - FK to SHELFTYPE.
      LocationX / LocationY- Grid coordinates for layout rendering.
      OrientationHorizontal- 1 = horizontal layout, 0 = vertical.
  Constraints:
      UNIQUE(LocationX, LocationY) prevents positional overlap.
      FK_SHELF_SHELFTYPE cascades deletes (dev convenience).
  Index Strategy:
      Separate NCI on Number supports ordered listings & direct numeric lookups.
  Future:
      - Consider UNIQUE(Number) if re-use must be prevented.
      - Add Active flag for soft retirement.
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
      Stores tenant/customer identity & contact data for rental contracts.
  Columns:
      Id, FirstName, LastName - Identity attributes.
      Address, PostalCode, City - Contact location (optional).
      Email - Optional; enforced unique when provided (NULLs allowed).
      PhoneNumber - Optional contact channel.
      Status - Arbitrary state (e.g., Active / Inactive).
  Constraints:
      UQ_SHELF_TENANT_EMAIL enforces uniqueness across non-null emails.
  Future:
      - Add CreatedAt / UpdatedAt for auditing.
      - Introduce Status domain table for controlled vocabulary.
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
      Contract header capturing a tenant's rental period & lifecycle state.
  Columns:
      Id             - PK.
      ShelfTenantId  - FK to SHELFTENANT.
      ContractNumber - Human-friendly sequential unique number (IDENTITY).
      StartDate      - Planned start (inclusive).
      EndDate        - Planned end (inclusive).
      CancelledAt    - If set, earlier termination date (inclusive).
  Business Rules (application enforced):
      - StartDate <= EndDate.
      - CancelledAt BETWEEN StartDate AND EndDate.
      - Contracts for same tenant should not overlap (unless business allows).
  Index Strategy:
      Composite NCI (see IX_ShelfTenantContract_Tenant_Contract_Dates) supports date range filtering
      + ordering by ContractNumber.
  Future:
      - Add Status or Reason codes for cancellation classification.
      - Add CreatedAt / CreatedBy for audit.
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
      Associates one or more shelves with a contract header.
  Columns:
      Id                     - PK.
      ShelfTenantContractId  - FK to SHELFTENANTCONTRACT.
      ShelfId                - Shelf being rented.
      LineNumber             - Sequential line ordering (unique within contract).
      PricePerMonth          - Standard monthly price.
      PricePerMonthSpecial   - Optional negotiated override (NULL => use standard tier pricing).
  Notes:
      - No enforcement preventing the same ShelfId across overlapping contracts; application logic
        must ensure exclusivity if required.
  Index Strategy:
      Covering NCI includes pricing fields to avoid key lookups (see IX_ShelfTenantContractLine_Contract_Shelf).
  Future:
      - Add EffectiveFrom / EffectiveTo for mid-contract shelf changes.
      - Add DiscountReason / Notes metadata.
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
  TABLE: SALESRECEIPT
  Purpose:
      Header record for a sales transaction (e.g., monthly rental charge batch or POS receipt).
  Columns:
      Id              - PK.
      ReceiptNumber   - Sequential identity for user reference.
      IssuedAt        - Timestamp of issuance.
      TotalAmount     - Gross (or net depending on business rule—see domain decision).
      TaxAmount       - Tax component (redundant; keep calculation logic consistent).
      PaidByCash      - 1 if cash method used.
      PaidByMobile    - 1 if mobile payment method used (non-exclusive).
  Notes:
      - Currently no FK to contract; add ShelfTenantContractId if correlating receipts to a contract.
      - Payment method flags could later be replaced by a normalized table or bitmask.
  Future:
      - Add NetAmount for explicit separation of tax vs net base.
      - Add CurrencyCode for multi-currency environments.
      - Add CHECK enforcing at least one payment flag = 1.
***************************************************************************************************/
CREATE TABLE [dbo].[SALESRECEIPT] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ReceiptNumber] INT IDENTITY(1,1) NOT NULL UNIQUE,
    [IssuedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [TotalAmount] DECIMAL(18,2) NOT NULL,
    [TaxAmount] DECIMAL(18,2) NOT NULL,
    [PaidByCash] BIT NOT NULL CONSTRAINT DF_SalesReceipt_PaidByCash DEFAULT(1),
    [PaidByMobile] BIT NOT NULL CONSTRAINT DF_SalesReceipt_PaidByMobile DEFAULT(0),
    CONSTRAINT CK_SalesReceipt_ExactlyOnePayment
        CHECK (
              ([PaidByCash] = 1 AND [PaidByMobile] = 0)
           OR ([PaidByCash] = 0 AND [PaidByMobile] = 1)
        )
);
GO

/***************************************************************************************************
  TABLE: SALESRECEIPTLINE
  Purpose:
      Line-level monetary entries belonging to a SALESRECEIPT.
  Columns:
      Id             - PK.
      ShelfNumber    - Snapshot of shelf number (denormalized to preserve historical view).
      SalesReceiptId - FK to SALESRECEIPT.
      UnitPrice      - Monetary value (interpretation depends on header policy: net or gross).
  Notes:
      - Extend with Quantity, TaxRateId, TaxAmount, LineNumber, Description as domain matures.
  Index Strategy:
      NCI on SalesReceiptId supports efficient parent->child retrieval.
  Future:
      - Add constraint verifying UnitPrice >= 0.
      - Consider storing ShelfId instead (plus snapshot fields) for traceability.
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
INSERT [dbo].[VERSIONINFO] ([Id], [Version], [AppliedAt]) VALUES (1, 'Initial', CAST('2025-10-06' AS date));
GO

/***************************************************************************************************
  INDEXES
  Rationale Summary:
    IX_ShelfTenantContract_Tenant_Contract_Dates
        - Pattern: WHERE ShelfTenantId = ? AND StartDate/EndDate range logic (active windows).
        - ContractNumber included for ordering / efficient drill-down.
        - CancelledAt INCLUDED so COALESCE/filters avoid lookups.
    IX_ShelfTenantContractLine_Contract_Shelf
        - Supports joins contract -> shelves with pricing projection (covering).
    IX_Shelf_Number
        - Supports ordered shelf list & direct numeric searches.
    IX_ShelfTenant_Email
        - Fast lookup by email (admin / login flows).
    IX_SalesReceip_IssuedAt
        - Enables chronological paging (IssuedAt, ReceiptNumber tiebreak).
    IX_SalesReceiptLine_SalesReceipt
        - Efficient retrieval of all lines for a receipt.
  Future:
    - Consider filtered indexes for active (non-cancelled) contracts.
    - Potential index on SHELFTENANT(Status) if frequently filtered by status.
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