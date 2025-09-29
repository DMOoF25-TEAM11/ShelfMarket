---------------------------------------------------------
--------------             DDL             --------------
---------------------------------------------------------
/*
    File:        DDL.v1.sql
    Purpose:     Complete rebuild script for the ShelfMarket_Dev database (development only).
    Safety:      This script DROPS and RECREATES the database. Do NOT run in production.

    Conventions:
        - Naming:
            * Tables: Upper snake case to mirror existing physical conventions.
            * Constraints: CK_<Table>_<Meaning>, FK_<Parent>, UQ_<Table>_<Columns>, IX_* for indexes.
        - All temporal business columns use DATE where time-of-day is irrelevant.
        - All GUID PKs use NEWID() (not sequential) because write hotspot risk is minimal at dev scale.
        - INT IDENTITY used for human-facing incrementing numbers (e.g. ContractNumber, ReceiptNumber).
        - Monetary precision: DECIMAL(18,2) for currency amounts; consider money only if locale invariance needed.
        - Effective dating pattern: (EffectiveFrom, EffectiveTo NULLable) for historical reconstruction.

    Performance / Design Considerations (summary):
        - Targeted nonclustered indexes created only for confirmed query patterns (filtered, range seeks, covering).
        - Commission + VAT rates: point-in-time retrieval via TOP(1) ORDER BY EffectiveFrom DESC pattern.
        - SALESRECEIPT totals persisted (denormalized) – ensure business layer centralizes tax logic.
        - For large scale, consider partitioning temporal tables and adding filtered indexes on active rows.

    Testing / Dev Workflow:
        - Script is idempotent only at database scope (drops & recreates). Individual object re-runs not supported here.
        - Seed data limited to initial structural version marker (VERSIONINFO); bulk seed handled by separate DML script.

    Change Log:
        30-09-2024  v1.0  Initial version.
*/

-- Treats double quotes (") as identifier delimiters (object names), not as string delimiters.
SET QUOTED_IDENTIFIER ON;
GO

/***************************************************************************************************
  SECTION: Database Reset (Development Only)
  - Forces single user, drops db if exists, recreates a clean environment.
  - DO NOT include this section in production deployment automation.
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
      Tracks applied schema/data version markers for manual / lightweight migration traceability.
  Columns:
      Id          - Static key (e.g., always 1 for current baseline) or sequence if multiple records kept.
      Version     - Semantic version or descriptive label (e.g., 'v1.0.0-dml-seed').
      AppliedAt   - Timestamp when record inserted (defaults to current date/time).
  Notes:
      Not a replacement for formal migrations; acts as a simple checkpoint reference for dev/testing.
***************************************************************************************************/
CREATE TABLE [dbo].[VERSIONINFO] (
    [Id] INT NOT NULL PRIMARY KEY,
    [Version] NVARCHAR(50) NOT NULL,
    [AppliedAt] DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/***************************************************************************************************
  TABLE: VATRATES
  Logical Concept:
      Tax (VAT) rate definitions with effective dating for audit reproducibility.
  Columns:
      Name          - Tax code / label ('VAT', 'Reduced', etc.).
      RatePercent   - Whole percent (e.g. 25.00 = 25%).
      EffectiveFrom - First date active (inclusive).
      EffectiveTo   - Last date active (inclusive) or NULL for open-ended.
  Constraints:
      UQ_TaxRates_Name_EffectiveFrom prevents duplicate same-name starting points.
  Query Pattern:
      SELECT TOP(1) *
      FROM VATRATES
      WHERE Name = 'VAT' AND EffectiveFrom <= @D
        AND (EffectiveTo IS NULL OR EffectiveTo >= @D)
      ORDER BY EffectiveFrom DESC;
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
      Captures organizational identity & VAT / special item tax feature toggles.
  Columns:
      IsTaxRegistered - Enables VAT logic.
      IsTaxUsedItem   - Enables "used item" commission-inclusive VAT calculation path (subset logic).
  Constraint:
      CK_CompanyInfo_TaxUsageConsistency ensures used-item flag cannot be 1 if not tax registered.
  Future:
      Consider normalization of addresses or multi-entity support.
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
  TABLE: STORERENT
  Purpose:
      Stores periodic facility/store rent amounts for internal profitability or cost allocation analysis.
  Columns:
      Rent           - Period rent amount (currency units).
      EffectiveFrom  - Start date for this rent value.
      EffectiveTo    - End date (NULL if current).
  Usage:
      Latest active rent at a date:
        SELECT TOP(1) Rent FROM STORERENT
         WHERE EffectiveFrom <= @D
           AND (EffectiveTo IS NULL OR EffectiveTo >= @D)
         ORDER BY EffectiveFrom DESC;
  Future Enhancements:
      - Add CostCenterId for multi-site.
      - Add CreatedAt / UpdatedAt for audit.
***************************************************************************************************/
CREATE TABLE [dbo].[STORERENT] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Rent] DECIMAL(18, 2) NOT NULL,
    [EffectiveFrom] DATE NOT NULL,
    [EffectiveTo] DATE NULL
);
GO

/***************************************************************************************************
  TABLE: STAFFSALERY  (Typo retained if intentional; consider renaming to STAFFSALARY)
  Purpose:
      Tracks staff salary values over time for internal allocation or margin analysis.
  Columns:
      StaffName      - Display name / key (not normalized).
      Salary         - Period salary (gross monthly or agreed base).
      EffectiveFrom  - Start date of this salary value.
      EffectiveTo    - End date (NULL if active).
  Notes:
      - Not linked to a staff dimension; add STAFF table if HR integration required.
      - Consider storing per-day cost derivation or standard hours for advanced allocation.
***************************************************************************************************/
CREATE TABLE [dbo].[STAFFSALERY] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [StaffName] NVARCHAR(255) NOT NULL,
    [Salary] DECIMAL(18, 2) NOT NULL,
    [EffectiveFrom] DATE NOT NULL,
    [EffectiveTo] DATE NULL
);
GO

/***************************************************************************************************
  TABLE: COMMISSION
  Purpose:
      Commission rate history for calculating commissionable portions of sales.
  Columns:
      RateProcent    - Whole percent (10 = 10%). (Consider DECIMAL(5,2) for precision.)
      EffectiveFrom / EffectiveTo - Active window.
  Notes:
      Overlapping periods not enforced—application must manage exclusivity.
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
      Tier-based shelf pricing (volume discount).
  Columns:
      MinShelvesInclusive - Threshold (inclusive) starting this tier.
      PricePerShelf       - Applied price for counts >= threshold until next tier.
  Notes:
      Does not include effective dating yet—current snapshot only.
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
      Classification / template for shelves (form factor, usage category).
  Constraint:
      UNIQUE(Name) prevents duplicate logical types.
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
      Physical rentable shelf entity (grid-based layout).
  Constraints:
      UNIQUE(LocationX, LocationY) prevents two shelves sharing same coordinates.
      FK -> SHELFTYPE (cascade delete allowed for dev convenience).
  Index Strategy:
      Separate NCI on Number (see IX_Shelf_Number) for ordering / filtering.
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
      Tenant/customer profile for shelf rental.
  Constraints:
      UQ_SHELF_TENANT_EMAIL enforces uniqueness for non-null emails.
  Notes:
      Status is a free-text domain—consider normalizing later.
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
      Contractual rental period summary for a tenant.
  Columns:
      ContractNumber - IDENTITY for human reference.
      CancelledAt    - Early termination marker (inclusive day).
  Index Strategy:
      Composite index supports tenant + temporal filtering (active windows) + ordering.
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
      Individual shelf allocations (line items) under a contract header.
  Constraints:
      Unique (ShelfTenantContractId, LineNumber) ensures deterministic ordering.
      FK cascade ensures lines removed with parent contract.
  Notes:
      PricePerMonthSpecial allows override of tier logic; NULL implies fallback pricing externally.
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
      Header for a point-of-sale or aggregated transaction.
  Columns:
      TotalAmount - Stored gross (or scenario dependent).
      VatAmount   - Stored VAT component (denormalized). Keep in sync with logic layer.
      Payment flags - Exactly one must be 1 (enforced CHECK).
  Notes:
      If tax logic changes historically, consider storing snapshot CommissionRate / VatRate per receipt.
***************************************************************************************************/
CREATE TABLE [dbo].[SALESRECEIPT] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ReceiptNumber] INT IDENTITY(1,1) NOT NULL UNIQUE,
    [IssuedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [VatAmount] DECIMAL(18,2) NOT NULL,
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
      Line-level prices associated with a receipt.
  Columns:
      ShelfNumber - Snapshot value (denormalized for historical view independent of shelf changes).
  Notes:
      Extend with LineNumber, descriptions, or per-line tax if mixed-rate items introduced.
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
  Only baseline version marker here (business seed loaded by separate DML script).
***************************************************************************************************/
INSERT [dbo].[VERSIONINFO] ([Id], [Version], [AppliedAt]) VALUES (1, 'Initial', CAST('2025-10-06' AS date));
GO

/***************************************************************************************************
  INDEXES
  Rationale:
      IX_ShelfTenantContract_Tenant_Contract_Dates
          - Composite (ShelfTenantId, ContractNumber, StartDate, EndDate) with CancelledAt included:
            * Supports tenant contract listing
            * Facilitates active-window queries (StartDate/EndDate range scans).
      IX_ShelfTenantContractLine_Contract_Shelf
          - (ShelfTenantContractId, ShelfId) includes pricing fields to avoid lookups in pricing joins.
      IX_Shelf_Number
          - Simple numeric lookups & ordered lists for UI.
      IX_ShelfTenant_Email
          - Fast unique email retrieval (support login/admin).
      IX_SalesReceip_IssuedAt
          - Chronological paging; ReceiptNumber as tiebreaker ensures stable order.
      IX_SalesReceiptLine_SalesReceipt
          - Parent -> child retrieval; essential for receipt detail expansion.

  Future Index Opportunities:
      - Filtered index for active (CancelledAt IS NULL) contracts.
      - Covering index for frequent shelf availability queries (StartDate/EndDate projections).
      - Add computed persisted tax base if mixed VAT regimes introduced (for analytics).
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

/***************************************************************************************************
  (OPTIONAL) EXTENDED PROPERTIES
  Uncomment to persist metadata in catalog for tooling (e.g., documentation generators).
  NOTE: Re-running after DROP/CREATE requires idempotent checks or wrapping logic.
***************************************************************************************************/
-- EXEC sp_addextendedproperty @name=N'Description', @value=N'VAT rate history table.',
--     @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'VATRATES';

-- EXEC sp_addextendedproperty @name=N'Description', @value=N'Sales receipt header with persisted totals.',
--     @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SALESRECEIPT';

-- EXEC sp_addextendedproperty @name=N'Description', @value=N'Line items for sales receipts.',
--     @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'SALESRECEIPTLINE';

-- Additional extended properties can be added similarly for other tables/columns.
