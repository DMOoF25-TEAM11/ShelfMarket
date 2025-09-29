---------------------------------------------------------
--------------             DML             --------------
---------------------------------------------------------
/*
    File:        DML.v1.sql
    Purpose:     Deterministic seed data for the ShelfMarket_Dev database after executing DDL.v1.sql.
    Scope:       Inserts baseline reference data (commission, VAT rate, pricing tiers, master data),
                 followed by a comprehensive realistic contract & shelf allocation dataset for 2025.
    Execution:   Run ONLY against a freshly rebuilt dev database (not production). Assumes all
                 objects created by DDL.v1.sql are present.
    Idempotency: NOT fully idempotent (hard-coded GUID primary keys). Re-execution without clearing
                 tables will raise PK / UNIQUE violations. This is acceptable for clean rebuild flow.
    Naming:      All GUID values are fixed for repeatability and may be referenced by tests.
    Notes:
        - Some business columns (e.g., COMPANYINFO.CvrNumber) are referenced here but the column
          does not exist in the current DDL (potential schema drift). Left intact; annotate below.
        - Pricing tiers are applied manually when constructing contract lines; no dynamic calculation
          logic is performed in this script.
        - Contract dataset models overlapping temporal scenarios (yearly, seasonal, phased usage).
        - Shelf exclusivity (no overlapping allocations across tenants) is manually respected.

    Change Log:
        30-09-2024  v1.0  Initial version.
*/

/***************************************************************************************************
  PLAN (PSEUDOCODE OVERVIEW)
  1. Set QUOTED_IDENTIFIER to ensure quoted identifiers behave consistently.
  2. Select target database (ShelfMarket_Dev).
  3. Seed global configuration & reference tables:
       a. COMMISSION (current active commission rate).
       b. VATRATES (standard VAT 25% open-ended).
       c. COMPANYINFO (base company profile) - NOTE: references CvrNumber not in DDL.
       d. SHELFPRICINGRULES (tier pricing: 1 / 2-3 / 4-5 shelves).
       e. SHELFTYPE definitions.
       f. SHELF master list with coordinates & orientation.
       g. SHELFTENANT master data (core + 15 fictive tenants).
  4. Insert contract headers & lines grouped by scenario:
       a. Long-term (full-year 2025) tenants (varied shelf counts => different price tiers).
       b. Multi-phase tenants with sequential contracts (seasonal / capacity change).
       c. Simple / single-period tenants, including cancellations & cross-year contracts.
  5. Maintain ordered, commented sections for readability & future maintenance.
  6. Keep all monetary values explicit (no reliance on defaults or calculations here).
  7. Provide inline commentary for each logical batch and notable business cases.
***************************************************************************************************/

-- Treats double quotes (") as identifier delimiters (object names), not as string delimiters.
SET QUOTED_IDENTIFIER ON;
GO

-- Target database selection (must exist from DDL script).
USE [ShelfMarket_Dev];
GO

/***************************************************************************************************
  SECTION: DATA PURGE (DEV ONLY)
  Empties all tables in FK-safe order, then reseeds IDENTITY columns.
  Notes:
    - FK cascade chains exist (e.g., SHELFTENANT -> CONTRACT -> CONTRACTLINE, SALESRECEIPT -> LINE,
      SHELFTYPE -> SHELF). We still delete child tables explicitly for clarity & deterministic rowcounts.
    - TRUNCATE not used because of FK constraints; DELETE is sufficient at this scale.
    - Receipt / Contract identity values are reseeded to start at 1 on next insert.
***************************************************************************************************/
SET XACT_ABORT ON;
BEGIN TRAN;

    -- Child-first (explicit, though some cascades would handle this).
    DELETE FROM dbo.SALESRECEIPTLINE;
    DELETE FROM dbo.SALESRECEIPT;

    DELETE FROM dbo.SHELFTENANTCONTRACTLINE;
    DELETE FROM dbo.SHELFTENANTCONTRACT;
    DELETE FROM dbo.SHELFTENANT;

    DELETE FROM dbo.SHELF;             -- FK to SHELFTYPE (cascade defined, still explicit)
    DELETE FROM dbo.SHELFTYPE;

    DELETE FROM dbo.SHELFPRICINGRULES;
    DELETE FROM dbo.COMMISSION;
    DELETE FROM dbo.VATRATES;
    DELETE FROM dbo.COMPANYINFO;
    DELETE FROM dbo.VERSIONINFO;       -- Remove version marker so seed can set a clean one

COMMIT;

-- Reseed identity tables (set to 0 so next insert becomes 1)
DBCC CHECKIDENT ('dbo.SALESRECEIPT', RESEED, 0);
DBCC CHECKIDENT ('dbo.SHELFTENANTCONTRACT', RESEED, 0);
GO



/***************************************************************************************************
  SECTION: COMMISSION
  - Stores the currently active commission rate (open-ended).
  - RateProcent stored as WHOLE percent (10 = 10%).
  - EffectiveTo NULL => still valid.
***************************************************************************************************/
INSERT [dbo].[COMMISSION] ([Id], [RateProcent], [EffectiveFrom], [EffectiveTo])
VALUES (N'0f8b6c2e-3d4e-4a5b-9c6d-7e8f9a0b1c2d', 10.0, CAST(GETDATE() AS date), NULL);
GO

/***************************************************************************************************
  SECTION: VAT RATE
  - Standard Danish VAT (moms) historically 25% since 1992 (illustrative backdated EffectiveFrom).
  - Open-ended current validity (EffectiveTo = NULL).
***************************************************************************************************/
INSERT [dbo].[VATRATES] ([Id],[Name],[RatePercent],[EffectiveFrom],[EffectiveTo])
VALUES (N'50096934-812b-438e-b285-972bc4d2ad2b', N'VAT', 25.00, '1992-01-01', NULL);
GO

/***************************************************************************************************
  SECTION: COMPANY INFO
  - Base legal entity configuration.
  - NOTE: Column [CvrNumber] is referenced here but DOES NOT exist in the DDL definition of
          COMPANYINFO at time of writing. This will cause an error unless DDL is updated.
          Retained intentionally; adjust either DDL (add CvrNumber NVARCHAR(?)) or remove column here.
***************************************************************************************************/
INSERT [dbo].[COMPANYINFO] (
    [Id], [Name], [Address], [PostalCode], [Email], [City],
    [PhoneNumber],  [CvrNumber], [IsTaxRegistered], [IsTaxUsedItem]
) VALUES (
    N'9a1b2c3d-4e5f-6789-abcd-ef0123456789',
    N'Middelby Reolmarked',
    N'Hovedgaden 12',
    N'1234',
    N'info@middelbyreolmarked.dk',
    N'Middelby',
    N'12345678',
    N'12345678', -- CvrNumber (schema mismatch warning above)
    1,
    1
);
GO

/***************************************************************************************************
  SECTION: PRICING RULES (Tiered Shelf Pricing)
    Tier Logic (applied manually in contract line pricing):
      1 shelf        => 850.00
      2-3 shelves    => 825.00 each
      4-5 shelves    => 800.00 each
  - Future enhancement: Add temporal validity & automated derivation during seeding.
***************************************************************************************************/
INSERT [dbo].[SHELFPRICINGRULES] ([Id], [MinShelvesInclusive], [PricePerShelf])
VALUES (N'1e8f4c3e-2d6e-4f4b-9a4e-0c5f1b6e7a8b', 1, 850);
INSERT [dbo].[SHELFPRICINGRULES] ([Id], [MinShelvesInclusive], [PricePerShelf])
VALUES (N'2b7e6d4f-3c7f-4e5a-8b9c-1d2e3f4a5b6c', 2, 825);
INSERT [dbo].[SHELFPRICINGRULES] ([Id], [MinShelvesInclusive], [PricePerShelf])
VALUES (N'3c8f7e5a-4d8f-5f6b-9c0d-2e3f4a5b6c7d', 4, 800);
GO

/***************************************************************************************************
  SECTION: SHELF TYPES
  - Base classification for physical configuration (examples only).
***************************************************************************************************/
INSERT [dbo].[SHELFTYPE] ([Id], [Name], [Description])
VALUES (N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', N'3 hylder og en stang', N'Egnet til tøj');
INSERT [dbo].[SHELFTYPE] ([Id], [Name], [Description])
VALUES (N'bcc9f172-052f-466d-b63c-e9901a6fee7d', N'6 hylder', N'Standard reol');
GO

/***************************************************************************************************
  SECTION: SHELVES
  - Full spatial grid & orientation seeding.
  - UNIQUE(LocationX, LocationY) enforced by schema ensures no coordinate collisions.
  - OrientationHorizontal: 1 = horizontal layout, 0 = vertical (UI hint).
  - Shelf numbers intentionally non-sequential geographically to simulate realistic layout.
***************************************************************************************************/
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'0859434c-dff5-4947-9084-029bc1956543', 60, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 3, 16, 1);
INSERT [dbo].[SHELF] VALUES (N'c0c8c483-6cd9-45db-9e74-0625fed3cd26', 41, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 11, 1);
INSERT [dbo].[SHELF] VALUES (N'a48ce1e6-bc70-41b8-9691-076bfd1d9699', 47, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 5, 12, 1);
INSERT [dbo].[SHELF] VALUES (N'dcaea195-166b-41f4-9fa3-08ddf7f10a1c', 18, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 9, 0, 1);
INSERT [dbo].[SHELF] VALUES (N'aacafaca-79e9-4b90-9d7f-09515c8fbeab', 65, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 13, 16, 1);
INSERT [dbo].[SHELF] VALUES (N'8db7ffb9-88a5-46b9-91f8-0e5fcf530c7a', 77, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 24, 0);
INSERT [dbo].[SHELF] VALUES (N'c9ba656d-6f8c-43f3-baf5-0fc0b70782d1', 80, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 24, 0);
INSERT [dbo].[SHELF] VALUES (N'e2f6994c-d218-459b-ada3-1006cd343429', 15, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 0, 1);
INSERT [dbo].[SHELF] VALUES (N'1119e9f4-be6a-4ca8-bb18-15356c6acf05', 46, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 12, 1);
INSERT [dbo].[SHELF] VALUES (N'86a37313-774a-4d8c-a41e-15b6af806ae3', 63, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 9, 16, 1);
INSERT [dbo].[SHELF] VALUES (N'773d9c52-240d-4afe-8413-1b6379c7cfb7', 42, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 9, 11, 1);
INSERT [dbo].[SHELF] VALUES (N'03fec3bc-06b2-428c-9246-20a44b754c9d', 35, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 9, 8, 1);
INSERT [dbo].[SHELF] VALUES (N'eebfa5d4-8330-407f-a777-21e6e69177f5', 43, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 11, 11, 1);
INSERT [dbo].[SHELF] VALUES (N'78339537-0ffe-46bd-b608-221d4e609bc7', 13, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 2, 0);
INSERT [dbo].[SHELF] VALUES (N'26188fa4-99f0-439a-af3e-22b0ea57cbb5', 36, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 11, 8, 1);
INSERT [dbo].[SHELF] VALUES (N'0f2c00e1-73d4-4f7d-ae42-244d58f366a9', 79, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 6, 24, 0);
INSERT [dbo].[SHELF] VALUES (N'8a7747b4-563a-45a3-b96e-37ef87ca34f5', 3, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 22, 0);
INSERT [dbo].[SHELF] VALUES (N'bc030769-1b59-4966-bbe8-3a60e8ef1a36', 9, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 0, 10, 0);
INSERT [dbo].[SHELF] VALUES (N'e2a487bd-7ff5-4835-bba4-3c9d6ec79a5f', 55, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 15, 1);
INSERT [dbo].[SHELF] VALUES (N'26806c27-597c-47b0-b423-3dfaf4441e18', 19, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 3, 3, 1);
INSERT [dbo].[SHELF] VALUES (N'8e69abb0-0702-4c5b-9bcb-3f2f95d4e00d', 20, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 5, 3, 1);
INSERT [dbo].[SHELF] VALUES (N'08854ca0-f5e5-4546-8227-3fbb14020fb1', 72, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 3, 20, 1);
INSERT [dbo].[SHELF] VALUES (N'974a4367-095f-408d-b65b-414028cd9169', 76, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 11, 20, 1);
INSERT [dbo].[SHELF] VALUES (N'0e229310-ae22-4b8b-86ec-41bf1d181aa8', 66, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 15, 16, 1);
INSERT [dbo].[SHELF] VALUES (N'2ed4175a-fdb1-4308-8718-46e6fe748b3c', 51, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 13, 12, 1);
INSERT [dbo].[SHELF] VALUES (N'7e3aea6d-5859-44f1-af4f-4e9805c178aa', 11, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 6, 0);
INSERT [dbo].[SHELF] VALUES (N'64f4d5d3-3b56-4666-ba9e-4f64cf7be3fc', 69, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 19, 1);
INSERT [dbo].[SHELF] VALUES (N'44100bfd-1bb9-4703-bf46-512baa1fc9a4', 37, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 13, 8, 1);
INSERT [dbo].[SHELF] VALUES (N'319daf5e-a271-426d-8c0a-54f3e78c792a', 68, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 5, 19, 1);
INSERT [dbo].[SHELF] VALUES (N'bb521441-b716-4b92-871f-5a5045aa294d', 70, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 9, 19, 1);
INSERT [dbo].[SHELF] VALUES (N'0cc975c2-b583-4911-828b-5e4104f0e1d4', 8, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 12, 0);
INSERT [dbo].[SHELF] VALUES (N'e3c7af28-d962-4be9-a66b-60f841514522', 61, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 5, 16, 1);
INSERT [dbo].[SHELF] VALUES (N'e93c3677-40bd-4871-a0d1-669798d53b73', 4, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 20, 0);
INSERT [dbo].[SHELF] VALUES (N'38555f31-ee5c-4381-8697-6cd0c4504481', 62, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 16, 1);
INSERT [dbo].[SHELF] VALUES (N'b5e4df40-39f8-457a-8ebd-6eab9ab3d284', 73, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 5, 20, 1);
INSERT [dbo].[SHELF] VALUES (N'8afa8b4f-e9f4-4c32-9f2e-701b018b7c12', 26, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 5, 7, 1);
INSERT [dbo].[SHELF] VALUES (N'1a30a17f-50bf-45c4-8e8b-7030dfcb4919', 5, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 0, 18, 0);
INSERT [dbo].[SHELF] VALUES (N'8fc52704-eada-4fa6-a29d-7dc048409078', 39, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 3, 11, 1);
INSERT [dbo].[SHELF] VALUES (N'39fa1466-a7ec-4c89-9622-7e35982e9879', 32, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 8, 1);
INSERT [dbo].[SHELF] VALUES (N'cddc024a-bfe2-4d71-84c1-80474f1e3db2', 52, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 15, 12, 1);
INSERT [dbo].[SHELF] VALUES (N'21dd4e79-d7a4-4f6d-b23b-848b87f31f0a', 74, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 7, 20, 1);
INSERT [dbo].[SHELF] VALUES (N'e38b5d62-ae1b-4e6d-ba7d-8725059d7538', 64, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 11, 16, 1);
INSERT [dbo].[SHELF] VALUES (N'4820fa79-9c7a-4859-89cf-88b818ff7f1f', 6, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 16, 0);
INSERT [dbo].[SHELF] VALUES (N'178f6622-384a-4bc2-83a1-8d116cc6a284', 57, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 11, 15, 1);
INSERT [dbo].[SHELF] VALUES (N'b7c8c5b1-dd52-4831-9893-93b27d019fe5', 50, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 11, 12, 1);
INSERT [dbo].[SHELF] VALUES (N'e2ad11ea-69ac-420e-8681-9b869e553d32', 31, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 15, 7, 1);
INSERT [dbo].[SHELF] VALUES (N'2706514b-9c92-4a34-af5f-9e10611f2496', 58, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 13, 15, 1);
INSERT [dbo].[SHELF] VALUES (N'd40cd212-8ab6-4e82-9a83-9ee9541da21d', 75, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 9, 20, 1);
INSERT [dbo].[SHELF] VALUES (N'58672ae3-2e30-488d-91eb-9fca37d8bfac', 27, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 7, 1);
INSERT [dbo].[SHELF] VALUES (N'0f8fcef8-10a8-4987-99ae-a459605f00aa', 21, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 3, 1);
INSERT [dbo].[SHELF] VALUES (N'45a0d5bc-4b02-412d-a80a-a4b3a2f15703', 17, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 7, 0, 1);
INSERT [dbo].[SHELF] VALUES (N'ed2667f9-d966-4a0e-8969-a6fb710e9842', 28, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 9, 7, 1);
INSERT [dbo].[SHELF] VALUES (N'fb4a75db-e13a-4af7-ac3d-aa3b946d65d7', 23, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 5, 4, 1);
INSERT [dbo].[SHELF] VALUES (N'95db7108-b1a2-4330-a78f-aac5b16e6faa', 14, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 1, 0, 1);
INSERT [dbo].[SHELF] VALUES (N'f7005ae6-5fa2-40de-a3db-abc8c5f4bdbd', 16, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 5, 0, 1);
INSERT [dbo].[SHELF] VALUES (N'f8cb794e-c1ff-46ce-8724-acb193ce6fab', 24, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 4, 1);
INSERT [dbo].[SHELF] VALUES (N'a7d43cb1-d3fc-4951-a141-adf83645da8a', 44, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 13, 11, 1);
INSERT [dbo].[SHELF] VALUES (N'3b7a6dfd-bd95-4241-9a25-b7d54ed83b7d', 67, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 19, 1);
INSERT [dbo].[SHELF] VALUES (N'337ed60f-574f-4bb3-9fde-b8af6051dead', 56, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 9, 15, 1);
INSERT [dbo].[SHELF] VALUES (N'a49ec3f6-440d-4b52-9b66-b91be3fdf625', 45, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 15, 11, 1);
INSERT [dbo].[SHELF] VALUES (N'bcfea791-342a-499d-b464-ba136b6af1d7', 38, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 15, 8, 1);
INSERT [dbo].[SHELF] VALUES (N'f4f2bf24-3f94-4993-b3f0-baa633907bdd', 33, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 5, 8, 1);
INSERT [dbo].[SHELF] VALUES (N'99f5291a-4149-4bab-b78c-bbd7682300e4', 71, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 11, 19, 1);
INSERT [dbo].[SHELF] VALUES (N'c6d3ab2d-f611-4fea-b4c2-be345ad19996', 34, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 7, 8, 1);
INSERT [dbo].[SHELF] VALUES (N'73eed88a-a47c-4a58-ac36-cdcf2b278774', 49, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 9, 12, 1);
INSERT [dbo].[SHELF] VALUES (N'21b26b87-bd6d-4b82-b2be-d14da913d8dd', 7, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 14, 0);
INSERT [dbo].[SHELF] VALUES (N'bef1175e-93bc-498d-9a91-d784e9d0ff56', 59, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 15, 15, 1);
INSERT [dbo].[SHELF] VALUES (N'26ca65b5-bba5-4443-81a3-dc6944bdfa72', 53, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 3, 15, 1);
INSERT [dbo].[SHELF] VALUES (N'63038ccd-6112-4eff-9d68-dfe143a65e3a', 29, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 11, 7, 1);
INSERT [dbo].[SHELF] VALUES (N'60a383a5-b25f-4102-9d75-e06d5e315d57', 25, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 7, 1);
INSERT [dbo].[SHELF] VALUES (N'feb786b3-5874-4210-8f04-e46b8990cc43', 54, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 5, 15, 1);
INSERT [dbo].[SHELF] VALUES (N'8cacb038-a249-4c3c-8aa3-ea6bab9990f5', 40, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 5, 11, 1);
INSERT [dbo].[SHELF] VALUES (N'6ce82b56-dd88-49d8-9366-efa7722a0460', 1, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 26, 0);
INSERT [dbo].[SHELF] VALUES (N'f355e9fe-7e69-495c-9413-efc81c1ef120', 48, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 7, 12, 1);
INSERT [dbo].[SHELF] VALUES (N'1a9bbfe1-b3b5-4a7f-81de-f1b719d53ac2', 22, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 4, 1);
INSERT [dbo].[SHELF] VALUES (N'724a9777-a145-421f-b279-f35d33a9ddaa', 2, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 24, 0);
INSERT [dbo].[SHELF] VALUES (N'ba7c4a0e-d80d-4cf4-b4e1-f6d05e8deebc', 10, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 8, 0);
INSERT [dbo].[SHELF] VALUES (N'bfb4c96d-26cf-4348-a4d5-f8b98063239f', 12, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 4, 0);
INSERT [dbo].[SHELF] VALUES (N'0bb5433c-11a0-480c-af64-f98898cdd070', 30, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 13, 7, 1);
INSERT [dbo].[SHELF] VALUES (N'867af828-011f-4fcd-88b9-fea0d18f6b8e', 78, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 4, 24, 0);
GO

/***************************************************************************************************
  SECTION: TENANTS
  - Core + extended fictive dataset (mix of Active / Inactive).
  - Emails unique (enforced by constraint); some Inactive tenants have historical contracts only.
***************************************************************************************************/
INSERT [dbo].[SHELFTENANT] VALUES (N'f1e2d3c4-b5a6-4b7c-8d9e-0f1a2b3c4d5e', N'Anton', N'Mikkelsen', N'Third Street 3', N'9101', N'Odense', N'Anton@gmail.com', N'30303030', N'Inactive');
INSERT [dbo].[SHELFTENANT] VALUES (N'd4a5e8f1-6c2b-4c3a-9f4e-1a2b3c4d5e6f', N'Louise', N'Ebersbach', N'Some Street 1', N'1234', N'Copenhagen', N'Louise@gmail.com', N'10101010', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'a1b2c3d4-e5f6-4a3b-9c8d-7e6f5a4b3c2d', N'Peter',  N'Holm', N'Another Street 2', N'5678', N'Aarhus', N'Peter@gmail.com', N'20202020', N'Active');
GO

-- Extended fictive tenants (diverse geographic + status variance)
INSERT [dbo].[SHELFTENANT] VALUES (N'0f4c9d6b-2c11-4f0d-9c3f-2c6a7b8d9e10', N'Mette', N'Sørensen', N'Nørregade 12', N'5000', N'Odense C', N'mette.soerensen@shelfmail.dk', N'40404040', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'11a2b3c4-d5e6-4789-9abc-def012345601', N'Lars', N'Andersen', N'Vestergade 45', N'8000', N'Aarhus C', N'lars.andersen@shelfmail.dk', N'41414141', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'21b3c4d5-e6f7-489a-abcd-ef0123456702', N'Camilla', N'Nielsen', N'Åboulevarden 7', N'8000', N'Aarhus C', N'camilla.nielsen@shelfmail.dk', N'42424242', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'31c4d5e6-f7a8-49ab-bcde-f01234567803', N'Jonas', N'Kristensen', N'Strandvejen 101', N'2900', N'Hellerup', N'jonas.kristensen@shelfmail.dk', N'43434343', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'41d5e6f7-a8b9-40bc-cdef-012345678904', N'Emma', N'Thomsen', N'Østergade 3', N'1100', N'København K', N'emma.thomsen@shelfmail.dk', N'44444444', N'Inactive');
INSERT [dbo].[SHELFTENANT] VALUES (N'51e6f7a8-b9c0-41cd-def0-123456789005', N'Frederik', N'Larsen', N'Skovvej 9', N'9000', N'Aalborg', N'frederik.larsen@shelfmail.dk', N'45454545', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'61f7a8b9-c0d1-42de-ef01-234567890106', N'Sofie', N'Mortensen', N'Park Allé 25', N'8000', N'Aarhus C', N'sofie.mortensen@shelfmail.dk', N'46464646', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'71a8b9c0-d1e2-43ef-f012-345678901207', N'Henrik', N'Olesen', N'Bredgade 60', N'1260', N'København K', N'henrik.olesen@shelfmail.dk', N'47474747', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'81b9c0d1-e2f3-4401-0013-456789012308', N'Maria', N'Jensen', N'Havnegade 14', N'5000', N'Odense C', N'maria.jensen@shelfmail.dk', N'48484848', N'Inactive');
INSERT [dbo].[SHELFTENANT] VALUES (N'91c0d1e2-f3a4-4502-0124-567890123409', N'Kasper', N'Pedersen', N'Torvegade 2', N'1400', N'København K', N'kasper.pedersen@shelfmail.dk', N'49494949', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'a1d1e2f3-a4b5-4603-1235-67890123450a', N'Ida', N'Madsen', N'Engvej 33', N'2300', N'København S', N'ida.madsen@shelfmail.dk', N'51515151', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'b1e2f3a4-b5c6-4704-2346-78901234560b', N'Nicolai', N'Rasmussen', N'Jyllandsgade 5', N'9000', N'Aalborg', N'nicolai.rasmussen@shelfmail.dk', N'52525252', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'c1f3a4b5-c6d7-4805-3457-89012345670c', N'Line', N'Jørgensen', N'Rådhuspladsen 1', N'1550', N'København V', N'line.joergensen@shelfmail.dk', N'53535353', N'Active');
INSERT [dbo].[SHELFTENANT] VALUES (N'd1a4b5c6-d7e8-4906-4568-90123456780d', N'Oliver', N'Poulsen', N'Søndergade 18', N'8700', N'Horsens', N'oliver.poulsen@shelfmail.dk', N'54545454', N'Inactive');
INSERT [dbo].[SHELFTENANT] VALUES (N'e1b5c6d7-e8f9-4a07-5679-01234567890e', N'Julie', N'Karlsen', N'Stationsvej 4', N'4000', N'Roskilde', N'julie.karlsen@shelfmail.dk', N'55555555', N'Active');
GO

/***************************************************************************************************
  SECTION: CONTRACT DATA
  - Contracts model:
      * Long-term full-year agreements.
      * Sequential partial-year phases (capacity changes & seasonal usage).
      * Single-month seasonal / cancellation scenarios.
      * Cross-year (spanning into 2026) and early cancellation example (Maria).
  - Pricing applied explicitly based on tier at time of contract (manual consistency).
  - CancelledAt only populated for early termination (Maria’s contract).
  - Shelf reuse across phases always non-overlapping in time.
***************************************************************************************************/

/* ============================ LONG TERM YEAR CONTRACTS ============================ */
-- Peter (2025 full year) - 5 shelves (tier 4-5 => 800)
INSERT dbo.SHELFTENANTCONTRACT (Id,ShelfTenantId,StartDate,EndDate,CancelledAt)
VALUES ('10000000-0000-0000-0000-000000000001','a1b2c3d4-e5f6-4a3b-9c8d-7e6f5a4b3c2d','2025-01-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('10000000-0000-0000-0001-000000000001','10000000-0000-0000-0000-000000000001','dcaea195-166b-41f4-9fa3-08ddf7f10a1c',1,800.00,NULL),
 ('10000000-0000-0000-0001-000000000002','10000000-0000-0000-0000-000000000001','aacafaca-79e9-4b90-9d7f-09515c8fbeab',2,800.00,NULL),
 ('10000000-0000-0000-0001-000000000003','10000000-0000-0000-0000-000000000001','8db7ffb9-88a5-46b9-91f8-0e5fcf530c7a',3,800.00,NULL),
 ('10000000-0000-0000-0001-000000000004','10000000-0000-0000-0000-000000000001','c9ba656d-6f8c-43f3-baf5-0fc0b70782d1',4,800.00,NULL),
 ('10000000-0000-0000-0001-000000000005','10000000-0000-0000-0000-000000000001','d40cd212-8ab6-4e82-9a83-9ee9541da21d',5,800.00,NULL);

-- Sofie (2025) - 2 shelves (tier 2-3 => 825)
INSERT dbo.SHELFTENANTCONTRACT VALUES ('10000000-0000-0000-0000-000000000002','61f7a8b9-c0d1-42de-ef01-234567890106','2025-01-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('10000000-0000-0000-0002-000000000001','10000000-0000-0000-0000-000000000002','0e229310-ae22-4b8b-86ec-41bf1d181aa8',1,825.00,NULL),
 ('10000000-0000-0000-0002-000000000002','10000000-0000-0000-0000-000000000002','2ed4175a-fdb1-4308-8718-46e6fe748b3c',2,825.00,NULL);

-- Henrik (2025) - 1 shelf (tier 1 => 850)
INSERT dbo.SHELFTENANTCONTRACT VALUES ('10000000-0000-0000-0000-000000000003','71a8b9c0-d1e2-43ef-f012-345678901207','2025-01-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('10000000-0000-0000-0003-000000000001','10000000-0000-0000-0000-000000000003','99f5291a-4149-4bab-b78c-bbd7682300e4',1,850.00,NULL);

-- Julie (2025) - 3 shelves (tier 2-3 => 825)
INSERT dbo.SHELFTENANTCONTRACT VALUES ('10000000-0000-0000-0000-000000000004','e1b5c6d7-e8f9-4a07-5679-01234567890e','2025-01-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('10000000-0000-0000-0004-000000000001','10000000-0000-0000-0000-000000000004','39fa1466-a7ec-4c89-9622-7e35982e9879',1,825.00,NULL),
 ('10000000-0000-0000-0004-000000000002','10000000-0000-0000-0000-000000000004','0bb5433c-11a0-480c-af64-f98898cdd070',2,825.00,NULL),
 ('10000000-0000-0000-0004-000000000003','10000000-0000-0000-0000-000000000004','63038ccd-6112-4eff-9d68-dfe143a65e3a',3,825.00,NULL);

/* ============================ SEQUENTIAL / MULTI-PHASE TENANTS ============================ */
-- Lars: capacity changes across the year (5 shelves -> 2 -> 1) demonstrating tier transitions.
INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000001','11a2b3c4-d5e6-4789-9abc-def012345601','2025-01-01','2025-06-30',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0001-000000000001','20000000-0000-0000-0000-000000000001','773d9c52-240d-4afe-8413-1b6379c7cfb7',1,800.00,NULL),
 ('20000000-0000-0000-0001-000000000002','20000000-0000-0000-0000-000000000001','03fec3bc-06b2-428c-9246-20a44b754c9d',2,800.00,NULL),
 ('20000000-0000-0000-0001-000000000003','20000000-0000-0000-0000-000000000001','26188fa4-99f0-439a-af3e-22b0ea57cbb5',3,800.00,NULL),
 ('20000000-0000-0000-0001-000000000004','20000000-0000-0000-0000-000000000001','86a37313-774a-4d8c-a41e-15b6af806ae3',4,800.00,NULL),
 ('20000000-0000-0000-0001-000000000005','20000000-0000-0000-0000-000000000001','eebfa5d4-8330-407f-a777-21e6e69177f5',5,800.00,NULL);

INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000002','11a2b3c4-d5e6-4789-9abc-def012345601','2025-08-01','2025-10-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0002-000000000001','20000000-0000-0000-0000-000000000002','03fec3bc-06b2-428c-9246-20a44b754c9d',1,825.00,NULL),
 ('20000000-0000-0000-0002-000000000002','20000000-0000-0000-0000-000000000002','86a37313-774a-4d8c-a41e-15b6af806ae3',2,825.00,NULL);

INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000003','11a2b3c4-d5e6-4789-9abc-def012345601','2025-12-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0003-000000000001','20000000-0000-0000-0000-000000000003','03fec3bc-06b2-428c-9246-20a44b754c9d',1,850.00,NULL);

-- Louise: main season + December return (capacity reduction in December).
INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000010','d4a5e8f1-6c2b-4c3a-9f4e-1a2b3c4d5e6f','2025-03-01','2025-09-30',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0010-000000000001','20000000-0000-0000-0000-000000000010','c0c8c483-6cd9-45db-9e74-0625fed3cd26',1,825.00,NULL),
 ('20000000-0000-0000-0010-000000000002','20000000-0000-0000-0000-000000000010','a48ce1e6-bc70-41b8-9691-076bfd1d9699',2,825.00,NULL),
 ('20000000-0000-0000-0010-000000000003','20000000-0000-0000-0000-000000000010','1119e9f4-be6a-4ca8-bb18-15356c6acf05',3,825.00,NULL);

INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000011','d4a5e8f1-6c2b-4c3a-9f4e-1a2b3c4d5e6f','2025-12-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0011-000000000001','20000000-0000-0000-0000-000000000011','c0c8c483-6cd9-45db-9e74-0625fed3cd26',1,825.00,NULL),
 ('20000000-0000-0000-0011-000000000002','20000000-0000-0000-0000-000000000011','a48ce1e6-bc70-41b8-9691-076bfd1d9699',2,825.00,NULL);

-- Camilla: demonstrates capacity increase then reduction to single shelf (tier shift to 850).
INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000020','21b3c4d5-e6f7-489a-abcd-ef0123456702','2025-02-01','2025-04-30',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0020-000000000001','20000000-0000-0000-0000-000000000020','64f4d5d3-3b56-4666-ba9e-4f64cf7be3fc',1,825.00,NULL),
 ('20000000-0000-0000-0020-000000000002','20000000-0000-0000-0000-000000000020','44100bfd-1bb9-4703-bf46-512baa1fc9a4',2,825.00,null);

INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000021','21b3c4d5-e6f7-489a-abcd-ef0123456702','2025-05-01','2025-07-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0021-000000000001','20000000-0000-0000-0000-000000000021','319daf5e-a271-426d-8c0a-54f3e78c792a',1,825.00,NULL),
 ('20000000-0000-0000-0021-000000000002','20000000-0000-0000-0000-000000000021','bb521441-b716-4b92-871f-5a5045aa294d',2,825.00,NULL),
 ('20000000-0000-0000-0021-000000000003','20000000-0000-0000-0000-000000000021','e3c7af28-d962-4be9-a66b-60f841514522',3,825.00,NULL);

INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000022','21b3c4d5-e6f7-489a-abcd-ef0123456702','2025-09-01','2025-11-30',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0022-000000000001','20000000-0000-0000-0000-000000000022','e3c7af28-d962-4be9-a66b-60f841514522',1,850.00,NULL);

-- Jonas: late-year usage with December single shelf repricing.
INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000030','31c4d5e6-f7a8-49ab-bcde-f01234567803','2025-09-01','2025-10-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0030-000000000001','20000000-0000-0000-0000-000000000030','78339537-0ffe-46bd-b608-221d4e609bc7',1,825.00,NULL),
 ('20000000-0000-0000-0030-000000000002','20000000-0000-0000-0000-000000000030','26806c27-597c-47b0-b423-3dfaf4441e18',2,825.00,NULL);

INSERT dbo.SHELFTENANTCONTRACT VALUES ('20000000-0000-0000-0000-000000000031','31c4d5e6-f7a8-49ab-bcde-f01234567803','2025-12-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('20000000-0000-0000-0031-000000000001','20000000-0000-0000-0000-000000000031','78339537-0ffe-46bd-b608-221d4e609bc7',1,850.00,NULL);

/* ============================ SINGLE / SIMPLE CONTRACT TENANTS ============================ */
-- Mette: mid-year start (1 shelf).
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-000000000001','0f4c9d6b-2c11-4f0d-9c3f-2c6a7b8d9e10','2025-06-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-0001-000000000001','30000000-0000-0000-0000-000000000001','e2f6994c-d218-459b-ada3-1006cd343429',1,850.00,NULL);

-- Emma: single October month (inactive tenant use case).
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-000000000002','41d5e6f7-a8b9-40bc-cdef-012345678904','2025-10-01','2025-10-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-0002-000000000001','30000000-0000-0000-0000-000000000002','08854ca0-f5e5-4546-8227-3fbb14020fb1',1,850.00,NULL);

-- Frederik: cross-year contract (Nov 2025 - Oct 2026) 4 shelves (tier 4-5 => 800).
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-000000000003','51e6f7a8-b9c0-41cd-def0-123456789005','2025-11-01','2026-10-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-0003-000000000001','30000000-0000-0000-0000-000000000003','95db7108-b1a2-4330-a78f-aac5b16e6faa',1,800.00,NULL),
 ('30000000-0000-0000-0003-000000000002','30000000-0000-0000-0000-000000000003','f7005ae6-5fa2-40de-a3db-abc8c5f4bdbd',2,800.00,NULL),
 ('30000000-0000-0000-0003-000000000003','30000000-0000-0000-0000-000000000003','45a0d5bc-4b02-412d-a80a-a4b3a2f15703',3,800.00,NULL),
 ('30000000-0000-0000-0003-000000000004','30000000-0000-0000-0000-000000000003','8e69abb0-0702-4c5b-9bcb-3f2f95d4e00d',4,800.00,NULL);

-- Maria: early cancellation scenario (CancelledAt inside contract window).
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-000000000004','81b9c0d1-e2f3-4401-0013-456789012308','2025-01-01','2025-05-31','2025-04-20');
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-0004-000000000001','30000000-0000-0000-0000-000000000004','38555f31-ee5c-4381-8697-6cd0c4504481',1,800.00,NULL),
 ('30000000-0000-0000-0004-000000000002','30000000-0000-0000-0000-000000000004','e38b5d62-ae1b-4e6d-ba7d-8725059d7538',2,800.00,NULL),
 ('30000000-0000-0000-0004-000000000003','30000000-0000-0000-0000-000000000004','b5e4df40-39f8-457a-8ebd-6eab9ab3d284',3,800.00,NULL),
 ('30000000-0000-0000-0004-000000000004','30000000-0000-0000-0000-000000000004','21dd4e79-d7a4-4f6d-b23b-848b87f31f0a',4,800.00,NULL);

-- Kasper: expansion from 3 shelves to 5 later (tier shift 825 -> 800).
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-000000000005','91c0d1e2-f3a4-4502-0124-567890123409','2025-04-01','2025-06-30',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-0005-000000000001','30000000-0000-0000-0000-000000000005','3b7a6dfd-bd95-4241-9a25-b7d54ed83b7d',1,825.00,NULL),
 ('30000000-0000-0000-0005-000000000002','30000000-0000-0000-0000-000000000005','337ed60f-574f-4bb3-9fde-b8af6051dead',2,825.00,NULL),
 ('30000000-0000-0000-0005-000000000003','30000000-0000-0000-0000-000000000005','a49ec3f6-440d-4b52-9b66-b91be3fdf625',3,825.00,NULL);

INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-000000000006','91c0d1e2-f3a4-4502-0124-567890123409','2025-08-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-0006-000000000001','30000000-0000-0000-0000-000000000006','3b7a6dfd-bd95-4241-9a25-b7d54ed83b7d',1,800.00,NULL),
 ('30000000-0000-0000-0006-000000000002','30000000-0000-0000-0000-000000000006','337ed60f-574f-4bb3-9fde-b8af6051dead',2,800.00,NULL),
 ('30000000-0000-0000-0006-000000000003','30000000-0000-0000-0000-000000000006','a49ec3f6-440d-4b52-9b66-b91be3fdf625',3,800.00,NULL),
 ('30000000-0000-0000-0006-000000000004','30000000-0000-0000-0000-000000000006','bcfea791-342a-499d-b464-ba136b6af1d7',4,800.00,NULL),
 ('30000000-0000-0000-0006-000000000005','30000000-0000-0000-0000-000000000006','a7d43cb1-d3fc-4951-a141-adf83645da8a',5,800.00,NULL);

-- Ida: steady medium-term (2 shelves tier 2-3 => 825)
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-000000000007','a1d1e2f3-a4b5-4603-1235-67890123450a','2025-03-01','2025-09-30',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-0007-000000000001','30000000-0000-0000-0000-000000000007','f4f2bf24-3f94-4993-b3f0-baa633907bdd',1,825.00,NULL),
 ('30000000-0000-0000-0007-000000000002','30000000-0000-0000-0000-000000000007','c6d3ab2d-f611-4fea-b4c2-be345ad19996',2,825.00,NULL);

-- Nicolai: cross-year shoulder season (3 shelves tier 2-3 => 825)
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-000000000008','b1e2f3a4-b5c6-4704-2346-78901234560b','2025-10-01','2026-03-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-0008-000000000001','30000000-0000-0000-0000-000000000008','73eed88a-a47c-4a58-ac36-cdcf2b278774',1,825.00,NULL),
 ('30000000-0000-0000-0008-000000000002','30000000-0000-0000-0000-000000000008','b7c8c5b1-dd52-4831-9893-93b27d019fe5',2,825.00,NULL),
 ('30000000-0000-0000-0008-000000000003','30000000-0000-0000-0000-000000000008','f355e9fe-7e69-495c-9413-efc81c1ef120',3,825.00,NULL);

-- Line: extended stable 5-shelf usage (tier 4-5 => 800)
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-000000000009','c1f3a4b5-c6d7-4805-3457-89012345670c','2025-05-01','2025-12-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-0009-000000000001','30000000-0000-0000-0000-000000000009','26ca65b5-bba5-4443-81a3-dc6944bdfa72',1,800.00,NULL),
 ('30000000-0000-0000-0009-000000000002','30000000-0000-0000-0000-000000000009','feb786b3-5874-4210-8f04-e46b8990cc43',2,800.00,NULL),
 ('30000000-0000-0000-0009-000000000003','30000000-0000-0000-0000-000000000009','bef1175e-93bc-498d-9a91-d784e9d0ff56',3,800.00,NULL),
 ('30000000-0000-0000-0009-000000000004','30000000-0000-0000-0000-000000000009','8cacb038-a249-4c3c-8aa3-ea6bab9990f5',4,800.00,NULL),
 ('30000000-0000-0000-0009-000000000005','30000000-0000-0000-0000-000000000009','8fc52704-eada-4fa6-a29d-7dc048409078',5,800.00,NULL);

-- Oliver: simple summer period (inactive tenant usage retained historically).
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-00000000000A','d1a4b5c6-d7e8-4906-4568-90123456780d','2025-07-01','2025-08-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-000A-000000000001','30000000-0000-0000-0000-00000000000A','e2ad11ea-69ac-420e-8681-9b869e553d32',1,850.00,NULL);

-- Anton: short early-year rental (inactive overall).
INSERT dbo.SHELFTENANTCONTRACT VALUES ('30000000-0000-0000-0000-00000000000B','f1e2d3c4-b5a6-4b7c-8d9e-0f1a2b3c4d5e','2025-02-01','2025-03-31',NULL);
INSERT dbo.SHELFTENANTCONTRACTLINE VALUES
 ('30000000-0000-0000-000B-000000000001','30000000-0000-0000-0000-00000000000B','ed2667f9-d966-4a0e-8969-a6fb710e9842',1,850.00,NULL);
GO



/***************************************************************************************************
  SECTION: SALES RECEIPT SYNTHETIC DATA (FIXED)
  Fixes:
    - Removed subqueries inside PRINT (Msg 1046) by using local variables.
    - Added leading semicolon before CTE block (Msg 156 near 'Plan').
    - Renamed CTE alias 'Plan' (potential keyword confusion) to LinePlanCte (Msg 156).
***************************************************************************************************/
SET NOCOUNT ON;
SET DATEFIRST 1; -- Monday = 1

DECLARE @StartDate date = '2025-01-01';
DECLARE @EndDate   date = CAST(GETDATE() AS date);  -- today

/* Calendar (Mon-Sat only) */
IF OBJECT_ID('tempdb..#Calendar') IS NOT NULL DROP TABLE #Calendar;
;WITH DaySeq AS (
    SELECT 0 AS d
    UNION ALL
    SELECT d + 1 FROM DaySeq WHERE DATEADD(DAY,d,@StartDate) < @EndDate
)
SELECT DateValue = DATEADD(DAY,d,@StartDate)
INTO #Calendar
FROM DaySeq
WHERE DATEPART(WEEKDAY, DATEADD(DAY,d,@StartDate)) BETWEEN 1 AND 6  -- Mon(1) .. Sat(6)
OPTION (MAXRECURSION 1000);

IF NOT EXISTS (SELECT 1 FROM #Calendar) RETURN;

/* Active shelves per date */
IF OBJECT_ID('tempdb..#ActiveShelves') IS NOT NULL DROP TABLE #ActiveShelves;
SELECT  c.DateValue, l.ShelfId
INTO    #ActiveShelves
FROM    #Calendar c
JOIN    dbo.SHELFTENANTCONTRACT sc
          ON sc.StartDate <= c.DateValue
         AND sc.EndDate   >= c.DateValue
         AND (sc.CancelledAt IS NULL OR sc.CancelledAt >= c.DateValue)
JOIN    dbo.SHELFTENANTCONTRACTLINE l
          ON l.ShelfTenantContractId = sc.Id;

IF OBJECT_ID('tempdb..#ActiveShelvesNorm') IS NOT NULL DROP TABLE #ActiveShelvesNorm;
SELECT DateValue,
       ShelfId,
       rn = ROW_NUMBER() OVER (PARTITION BY DateValue ORDER BY ShelfId),
       ShelfCount = COUNT(*) OVER (PARTITION BY DateValue)
INTO   #ActiveShelvesNorm
FROM   #ActiveShelves;

-- Remove calendar days with no active shelves
DELETE c
FROM #Calendar c
WHERE NOT EXISTS (SELECT 1 FROM #ActiveShelvesNorm a WHERE a.DateValue = c.DateValue);

/* Receipt seeds (≈100 / day) */
IF OBJECT_ID('tempdb..#ReceiptSeed') IS NOT NULL DROP TABLE #ReceiptSeed;
WITH Tally100 AS (
    SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects
)
SELECT
    ReceiptId    = NEWID(),
    IssuedAt     = DATEADD(SECOND,
                           9*3600 + (ABS(CHECKSUM(NEWID())) % (9*3600)), -- random second in 09:00-17:59
                           CAST(c.DateValue AS datetime)),
    DateValue    = c.DateValue,
    PaidByCash   = CASE WHEN (ABS(CHECKSUM(NEWID())) % 2) = 0 THEN 1 ELSE 0 END,
    PaidByMobile = NULL
INTO #ReceiptSeed
FROM #Calendar c
CROSS JOIN Tally100;

UPDATE #ReceiptSeed SET PaidByMobile = 1 - PaidByCash;

-- Safe PRINT without subqueries (previous error Msg 1046)
DECLARE @ReceiptCount int, @DayCount int;
SELECT @ReceiptCount = COUNT(*) FROM #ReceiptSeed;
SELECT @DayCount = COUNT(*) FROM #Calendar;
PRINT CONCAT('Generating ', @ReceiptCount, ' sales receipts over ', @DayCount, ' sales days.');

/* Insert receipts (totals updated later) */
INSERT dbo.SALESRECEIPT (Id, IssuedAt, VatAmount, PaidByCash, PaidByMobile)
SELECT ReceiptId, IssuedAt, 0.00, PaidByCash, PaidByMobile
FROM #ReceiptSeed
ORDER BY DateValue, IssuedAt, ReceiptId;  -- Ensures sequential chronological ReceiptNumber
GO

/* Line count plan */
IF OBJECT_ID('tempdb..#ReceiptLinePlan') IS NOT NULL DROP TABLE #ReceiptLinePlan;
;WITH LineBase AS (
    SELECT ReceiptId,
           DateValue,
           IssuedAt,
           rsRand = ABS(CHECKSUM(NEWID())) % 10000
    FROM #ReceiptSeed
),
LinePlanCte AS (
    SELECT ReceiptId,
           DateValue,
           IssuedAt,
           LineCount = CASE
               WHEN rsRand < 1000  THEN 1
               WHEN rsRand < 3500  THEN 2
               WHEN rsRand < 6500  THEN 3
               WHEN rsRand < 8500  THEN 4
               ELSE                    5
           END
    FROM LineBase
)
SELECT p.*,
       n.LineNumber
INTO #ReceiptLinePlan
FROM LinePlanCte p
JOIN (VALUES (1),(2),(3),(4),(5)) n(LineNumber) ON n.LineNumber <= p.LineCount;

/* Generate line prices & shelves */
IF OBJECT_ID('tempdb..#SalesLines') IS NOT NULL DROP TABLE #SalesLines;
SELECT
    LineId          = NEWID(),
    SalesReceiptId  = p.ReceiptId,
    ShelfNumber     = sh.Number,
    UnitPrice       = CAST(
                        CASE
                          WHEN rPriceBucket < 8000 THEN 10  + (rRange % 41)   -- 10-50 (80%)
                          WHEN rPriceBucket < 9500 THEN 51  + (rRange % 150)  -- 51-200 (15%)
                          ELSE                   201 + (rRange % 300)          -- 201-500 (5%)
                        END AS DECIMAL(18,2)),
    p.LineNumber
INTO #SalesLines
FROM #ReceiptLinePlan p
CROSS APPLY (
    SELECT rPriceBucket = ABS(CHECKSUM(NEWID())) % 10000,
           rRange       = ABS(CHECKSUM(NEWID())),
           rShelfPick   = ABS(CHECKSUM(NEWID()))
) r
JOIN #ActiveShelvesNorm a
     ON a.DateValue = p.DateValue
    AND a.rn = ((r.rShelfPick % a.ShelfCount) + 1)
JOIN dbo.SHELF sh ON sh.Id = a.ShelfId;

INSERT dbo.SALESRECEIPTLINE (Id, ShelfNumber, SalesReceiptId, UnitPrice)
SELECT LineId, ShelfNumber, SalesReceiptId, UnitPrice
FROM #SalesLines;

/* Update totals & tax (VAT 25% => 25/125 of gross) */
WITH Totals AS (
    SELECT SalesReceiptId,
           Gross = SUM(UnitPrice)
    FROM #SalesLines
    GROUP BY SalesReceiptId
)
UPDATE r
SET r.VatAmount   = ROUND(t.Gross * 25.00 / 125.00, 2)
FROM dbo.SALESRECEIPT r
JOIN Totals t ON t.SalesReceiptId = r.Id;

PRINT 'Sales receipt generation complete.';
SET NOCOUNT OFF;

/***************************************************************************************************
  REVISED VAT / COMMISSION TAX CALCULATION
  Business Rule:
    - Shelf/customer PRICE (UnitPrice) already includes COMMISSION.
    - COMMISSION itself is VAT-inclusive (you only remit VAT on the commission portion).
  Model (commission defined as % of seller net, embedded in price):
      Let:
         P  = UnitPrice (customer pays this)
         c  = Commission rate percent (e.g. 10)
         v  = VAT rate (e.g. 25% expressed as 0.25)
      Decomposition:
         SellerNet = P / (1 + c/100)
         CommissionGross = P - SellerNet = P * c / (100 + c)
         VAT (remitted) = CommissionGross * v / (1 + v)
         Previous logic (VAT on full price) overstated VAT.
  NOTE: If (in your domain) commission was actually defined as a percent of gross (rare here),
        use CommissionGross = P * (c/100) instead. Adjust formula accordingly.
***************************************************************************************************/
DECLARE @CommissionRate DECIMAL(9,4);
DECLARE @VatRate        DECIMAL(9,4);
DECLARE @VatFractionOnCommission DECIMAL(18,10); -- overall factor applied to UnitPrice
-- Get active commission (simple open-ended selection)
SELECT TOP(1) @CommissionRate = RateProcent
FROM dbo.COMMISSION
WHERE EffectiveFrom <= GETDATE()
  AND (EffectiveTo IS NULL OR EffectiveTo >= GETDATE())
ORDER BY EffectiveFrom DESC;

-- Get active VAT rate (whole percent -> convert to fraction)
SELECT TOP(1) @VatRate = RatePercent
FROM dbo.VATRATES
WHERE EffectiveFrom <= GETDATE()
  AND (EffectiveTo IS NULL OR EffectiveTo >= GETDATE())
ORDER BY EffectiveFrom DESC;

IF @CommissionRate IS NULL OR @VatRate IS NULL
BEGIN
    RAISERROR('Commission or VAT rate not found; cannot compute VAT on commission.',16,1);
    RETURN;
END

-- Convert VAT whole percent to fraction
SET @VatRate = @VatRate / 100.0;

-- Overall tax factor applied directly to UnitPrice:
-- (c/(100+c)) * (v/(1+v))
SET @VatFractionOnCommission =
    (@CommissionRate / (100.0 + @CommissionRate)) * (@VatRate / (1 + @VatRate));

/* Recompute receipt totals & VAT (only on commission portion) */
;WITH LineAgg AS (
    SELECT SalesReceiptId,
           Gross      = SUM(UnitPrice),
           TaxOnComm  = SUM(UnitPrice * @VatFractionOnCommission)
    FROM dbo.SALESRECEIPTLINE
    GROUP BY SalesReceiptId
)
UPDATE r
SET r.VatAmount   = ROUND(a.TaxOnComm, 2)
FROM dbo.SALESRECEIPT r
JOIN LineAgg a ON a.SalesReceiptId = r.Id;

PRINT CONCAT('Updated VAT using commission-inclusive model. Factor applied to line price: ',
             CAST(@VatFractionOnCommission AS VARCHAR(40)));

-- Further revise using unified business logic ( COMPANYINFO.IsTaxUsedItem / IsTaxRegistered )
-- Residual entries (if any) should all be no-VAT cases (Ida, Anton)
;WITH NoVatCases AS (
    SELECT r.Id
    FROM dbo.SALESRECEIPT r
    LEFT JOIN dbo.COMPANYINFO c ON 1=1  -- cross join to propagate single company row
    WHERE r.VatAmount = 0 
      AND (c.IsTaxUsedItem = 0 OR c.IsTaxRegistered = 0)
)
DELETE FROM dbo.SALESRECEIPT
WHERE Id IN (SELECT Id FROM NoVatCases);
GO



/***************************************************************************************************
  SECTION: SALES RECEIPT TAX & TOTAL CALCULATION (UNIFIED BUSINESS LOGIC)
  Business Rules (based on COMPANYINFO flags):
    1) IsTaxUsedItem = 1   (implies IsTaxRegistered = 1 by constraint)
         - UnitPrice already includes Commission and Commission is VAT-inclusive.
         - Only VAT due = VAT portion embedded inside the commission.
         - VAT factor per price:
              f_used = (c / (100 + c)) * ( (v/100) / (1 + v/100) )
            where c = commission %, v = VAT %.
         - TotalAmount (gross shown to customer) = SUM(UnitPrice).
         - VatAmount = SUM(UnitPrice) * f_used (rounded 2).
    2) IsTaxUsedItem = 0 AND IsTaxRegistered = 1  (normal VAT regime)
         - UnitPrice stored as NET (exclusive of VAT, commission not VAT-inclusive).
         - VAT due on full net price: VatAmount = Net * (v/100).
         - TotalAmount (gross) = Net + VatAmount.
    3) IsTaxRegistered = 0
         - No VAT. TotalAmount = SUM(UnitPrice). VatAmount = 0.

  Implementation details:
    - Runs AFTER lines inserted.
    - Single pass update; previous generic VAT updates removed.
    - Commission & VAT rates picked as “currently active” (GETDATE()).
      If historical accuracy per receipt IssuedAt is required, adapt the rate lookup to use IssuedAt.
***************************************************************************************************/

DECLARE @IsTaxUsedItem     bit;
DECLARE @IsTaxRegistered   bit;
DECLARE @CommissionRatePct decimal(9,4) = 0;
DECLARE @VatRatePct        decimal(9,4) = 0;

-- Company flags (assumes single row – refine if multiple company rows)
SELECT TOP(1)
    @IsTaxUsedItem   = IsTaxUsedItem,
    @IsTaxRegistered = IsTaxRegistered
FROM dbo.COMPANYINFO
ORDER BY Id;

-- Active commission (current date scope)
SELECT TOP(1) @CommissionRatePct = RateProcent
FROM dbo.COMMISSION
WHERE EffectiveFrom <= GETDATE()
  AND (EffectiveTo IS NULL OR EffectiveTo >= GETDATE())
ORDER BY EffectiveFrom DESC;

-- Active VAT (current date scope)
SELECT TOP(1) @VatRatePct = RatePercent
FROM dbo.VATRATES
WHERE EffectiveFrom <= GETDATE()
  AND (EffectiveTo IS NULL OR EffectiveTo >= GETDATE())
ORDER BY EffectiveFrom DESC;

-- Defensive defaults
IF @IsTaxUsedItem IS NULL SET @IsTaxUsedItem = 0;
IF @IsTaxRegistered IS NULL SET @IsTaxRegistered = 0;
IF @CommissionRatePct IS NULL SET @CommissionRatePct = 0;
IF @VatRatePct IS NULL SET @VatRatePct = 0;

-- Precompute factors
DECLARE @VatFractionOnCommission DECIMAL(18,10) = 0;  -- Case 1 factor (VAT only inside commission)
DECLARE @VatRateFraction        DECIMAL(18,10) = CASE WHEN @VatRatePct = 0 THEN 0 ELSE @VatRatePct / 100.0 END;

IF @IsTaxUsedItem = 1 AND @CommissionRatePct > 0 AND @VatRatePct > 0
BEGIN
    -- (c/(100+c)) * ( (v/100) / (1 + v/100) )
    SET @VatFractionOnCommission =
        (@CommissionRatePct / (100.0 + @CommissionRatePct)) *
        ( @VatRateFraction / (1 + @VatRateFraction) );
END

;WITH Base AS (
    SELECT SalesReceiptId, BaseSum = SUM(UnitPrice)
    FROM dbo.SALESRECEIPTLINE
    GROUP BY SalesReceiptId
)
UPDATE r
SET
    r.VatAmount =
        CASE
            WHEN @IsTaxRegistered = 0 THEN 0
            WHEN @IsTaxUsedItem = 1 THEN ROUND(b.BaseSum * @VatFractionOnCommission, 2)
            ELSE ROUND(b.BaseSum * @VatRateFraction, 2) -- normal VAT on full net base
        END
FROM dbo.SALESRECEIPT r
JOIN Base b ON b.SalesReceiptId = r.Id;

PRINT CONCAT(
    'Tax recompute applied. Scenario=',
    CASE
        WHEN @IsTaxUsedItem = 1 THEN 'UsedItem_CommissionVATOnly'
        WHEN @IsTaxRegistered = 1 THEN 'Normal_VAT_FullPrice'
        ELSE 'NoVAT_NotRegistered'
    END,
    '; CommissionRate=', @CommissionRatePct,
    '; VatRate=', @VatRatePct,
    '; FactorUsedItem=', FORMAT(@VatFractionOnCommission, '0.############')
);
GO
