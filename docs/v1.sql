---------------------------------------------------------
--------------             DDL             --------------
---------------------------------------------------------

-- Create database if it does not exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ShelfMarket_Dev1')
BEGIN
    CREATE DATABASE [ShelfMarket_Dev1];
END
GO

-- Use the database
USE [ShelfMarket_Dev1];
GO

-- Create table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'[SHELFTYPE]' AND TABLE_SCHEMA = N'dbo')
BEGIN
    CREATE TABLE [dbo].[SHELFTYPE] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Name] NVARCHAR(255) NOT NULL UNIQUE,
        [Description] NVARCHAR(200) NULL
    );
END
GO

-- Create SHELF table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SHELF' AND TABLE_SCHEMA = N'dbo')
BEGIN
    CREATE TABLE [dbo].[SHELF] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Number] INT NOT NULL,
        [ShelfTypeId] UNIQUEIDENTIFIER NOT NULL,
        [LocationX] INT NOT NULL,
        [LocationY] INT NOT NULL,
        [OrientationHorizontal] BIT NOT NULL CONSTRAINT DF_SHELF_OrientationHorizontal DEFAULT(1),
        CONSTRAINT [FK_SHELF_SHELFTTYPE] FOREIGN KEY ([ShelfTypeId])
            REFERENCES [dbo].[SHELFTYPE]([Id]),
            UNIQUE ([LocationX], [LocationY])
    );
END
GO

-- Create table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SHELFTENANT' AND TABLE_SCHEMA = N'dbo')
BEGIN
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
END
GO

-- Create table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SHELFRENTCONTRACT' AND TABLE_SCHEMA = N'dbo')
BEGIN
    CREATE TABLE [dbo].[SHELFRENTCONTRACT] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [ShelfTenantId] UNIQUEIDENTIFIER NOT NULL,
        [ContractNumber] INT NOT NULL UNIQUE,
        [StartDate] DATE NOT NULL,
        [EndDate] DATE NOT NULL,
        [Cancelled] DATE NULL,
        CONSTRAINT [FK_ShelfTenant] FOREIGN KEY ([ShelfTenantId]) REFERENCES [dbo].[HELFTENANT]([Id])
    );
END
GO

-- Create table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SHELFRENTCONTRACTLINE' AND TABLE_SCHEMA = N'[dbo]')
BEGIN
    CREATE TABLE [dbo].[SHELFRENTCONTRACTLINE] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [ShelfRentContractId] UNIQUEIDENTIFIER NOT NULL,
        [LineNumber] INT NOT NULL,
        [Description] NVARCHAR(255) NOT NULL,
        [Amount] DECIMAL(18, 2) NOT NULL,
        CONSTRAINT [UQ_ShelfRentContract_LineNumber] UNIQUE ([ShelfRentContractId], [LineNumber]),
        CONSTRAINT [FK_ShelfRentContract] FOREIGN KEY ([ShelfRentContractId]) REFERENCES [dbo].[SHELFRENTCONTRACT]([Id])
    );
END
GO



---------------------------------------------------------
--------------             DML             --------------
---------------------------------------------------------


INSERT [dbo].[SHELFTYPE] ([Id], [Name], [Description]) VALUES (N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', N'3 hylder og en stang', N'Egnet til tøj')
GO
INSERT [dbo].[SHELFTYPE] ([Id], [Name], [Description]) VALUES (N'bcc9f172-052f-466d-b63c-e9901a6fee7d', N'6 hylder', N'Standard reol')
GO


INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'0859434c-dff5-4947-9084-029bc1956543', 60, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 2, 10, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'c0c8c483-6cd9-45db-9e74-0625fed3cd26', 41, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 6, 16, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'a48ce1e6-bc70-41b8-9691-076bfd1d9699', 47, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 4, 15, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'aacafaca-79e9-4b90-9d7f-09515c8fbeab', 65, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 12, 10, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'8db7ffb9-88a5-46b9-91f8-0e5fcf530c7a', 77, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 2, 2, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'c9ba656d-6f8c-43f3-baf5-0fc0b70782d1', 80, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 2, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'e2f6994c-d218-459b-ada3-1006cd343429', 15, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 26, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'1119e9f4-be6a-4ca8-bb18-15356c6acf05', 46, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 2, 15, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'86a37313-774a-4d8c-a41e-15b6af806ae3', 63, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 8, 10, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'773d9c52-240d-4afe-8413-1b6379c7cfb7', 42, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 8, 16, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'03fec3bc-06b2-428c-9246-20a44b754c9d', 35, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 8, 18, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'eebfa5d4-8330-407f-a777-21e6e69177f5', 43, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 10, 16, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'78339537-0ffe-46bd-b608-221d4e609bc7', 13, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 24, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'26188fa4-99f0-439a-af3e-22b0ea57cbb5', 36, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 10, 18, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'0f2c00e1-73d4-4f7d-ae42-244d58f366a9', 79, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 6, 2, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'8a7747b4-563a-45a3-b96e-37ef87ca34f5', 3, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 4, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'bc030769-1b59-4966-bbe8-3a60e8ef1a36', 9, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 0, 16, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'e2a487bd-7ff5-4835-bba4-3c9d6ec79a5f', 55, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 6, 11, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'26806c27-597c-47b0-b423-3dfaf4441e18', 19, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 3, 23, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'8e69abb0-0702-4c5b-9bcb-3f2f95d4e00d', 20, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 5, 23, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'08854ca0-f5e5-4546-8227-3fbb14020fb1', 72, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 2, 6, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'974a4367-095f-408d-b65b-414028cd9169', 76, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 10, 6, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'0e229310-ae22-4b8b-86ec-41bf1d181aa8', 66, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 14, 10, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'2ed4175a-fdb1-4308-8718-46e6fe748b3c', 51, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 12, 15, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'7e3aea6d-5859-44f1-af4f-4e9805c178aa', 11, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 20, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'64f4d5d3-3b56-4666-ba9e-4f64cf7be3fc', 69, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 6, 7, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'44100bfd-1bb9-4703-bf46-512baa1fc9a4', 37, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 12, 18, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'319daf5e-a271-426d-8c0a-54f3e78c792a', 68, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 4, 7, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'bb521441-b716-4b92-871f-5a5045aa294d', 70, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 8, 7, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'0cc975c2-b583-4911-828b-5e4104f0e1d4', 8, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 14, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'e3c7af28-d962-4be9-a66b-60f841514522', 61, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 4, 10, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'e93c3677-40bd-4871-a0d1-669798d53b73', 4, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 6, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'38555f31-ee5c-4381-8697-6cd0c4504481', 62, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 6, 10, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'b5e4df40-39f8-457a-8ebd-6eab9ab3d284', 73, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 4, 6, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'8afa8b4f-e9f4-4c32-9f2e-701b018b7c12', 26, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 4, 19, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'1a30a17f-50bf-45c4-8e8b-7030dfcb4919', 5, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 0, 8, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'8fc52704-eada-4fa6-a29d-7dc048409078', 39, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 2, 16, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'39fa1466-a7ec-4c89-9622-7e35982e9879', 32, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 2, 18, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'cddc024a-bfe2-4d71-84c1-80474f1e3db2', 52, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 14, 15, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'21dd4e79-d7a4-4f6d-b23b-848b87f31f0a', 74, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 6, 6, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'e38b5d62-ae1b-4e6d-ba7d-8725059d7538', 64, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 10, 10, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'4820fa79-9c7a-4859-89cf-88b818ff7f1f', 6, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 10, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'178f6622-384a-4bc2-83a1-8d116cc6a284', 57, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 10, 11, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'b7c8c5b1-dd52-4831-9893-93b27d019fe5', 50, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 10, 15, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'e2ad11ea-69ac-420e-8681-9b869e553d32', 31, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 14, 19, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'2706514b-9c92-4a34-af5f-9e10611f2496', 58, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 12, 11, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'd40cd212-8ab6-4e82-9a83-9ee9541da21d', 75, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 8, 6, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'58672ae3-2e30-488d-91eb-9fca37d8bfac', 27, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 6, 19, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'0f8fcef8-10a8-4987-99ae-a459605f00aa', 21, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 23, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'45a0d5bc-4b02-412d-a80a-a4b3a2f15703', 17, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 7, 26, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'ed2667f9-d966-4a0e-8969-a6fb710e9842', 28, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 8, 19, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'fb4a75db-e13a-4af7-ac3d-aa3b946d65d7', 23, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 5, 22, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'95db7108-b1a2-4330-a78f-aac5b16e6faa', 14, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 1, 26, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'f7005ae6-5fa2-40de-a3db-abc8c5f4bdbd', 16, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 5, 26, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'f8cb794e-c1ff-46ce-8724-acb193ce6fab', 24, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 7, 22, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'a7d43cb1-d3fc-4951-a141-adf83645da8a', 44, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 12, 16, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'3b7a6dfd-bd95-4241-9a25-b7d54ed83b7d', 67, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 2, 7, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'337ed60f-574f-4bb3-9fde-b8af6051dead', 56, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 8, 11, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'a49ec3f6-440d-4b52-9b66-b91be3fdf625', 45, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 14, 16, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'bcfea791-342a-499d-b464-ba136b6af1d7', 38, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 14, 18, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'f4f2bf24-3f94-4993-b3f0-baa633907bdd', 33, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 4, 18, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'99f5291a-4149-4bab-b78c-bbd7682300e4', 71, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 10, 7, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'c6d3ab2d-f611-4fea-b4c2-be345ad19996', 34, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 6, 18, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'73eed88a-a47c-4a58-ac36-cdcf2b278774', 49, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 8, 15, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'21b26b87-bd6d-4b82-b2be-d14da913d8dd', 7, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 12, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'71bb86ee-6b17-4de5-9192-d2e7c9b2b56f', 18, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 8, 26, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'bef1175e-93bc-498d-9a91-d784e9d0ff56', 59, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 14, 11, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'26ca65b5-bba5-4443-81a3-dc6944bdfa72', 53, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 2, 11, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'63038ccd-6112-4eff-9d68-dfe143a65e3a', 29, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 10, 19, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'60a383a5-b25f-4102-9d75-e06d5e315d57', 25, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 2, 19, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'feb786b3-5874-4210-8f04-e46b8990cc43', 54, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 4, 11, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'8cacb038-a249-4c3c-8aa3-ea6bab9990f5', 40, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 4, 16, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'6ce82b56-dd88-49d8-9366-efa7722a0460', 1, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 0, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'f355e9fe-7e69-495c-9413-efc81c1ef120', 48, N'4aebd9cf-9cd7-4f6e-bb38-c79bf279334b', 6, 15, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'1a9bbfe1-b3b5-4a7f-81de-f1b719d53ac2', 22, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 22, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'724a9777-a145-421f-b279-f35d33a9ddaa', 2, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 2, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'ba7c4a0e-d80d-4cf4-b4e1-f6d05e8deebc', 10, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 18, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'bfb4c96d-26cf-4348-a4d5-f8b98063239f', 12, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 0, 22, 0)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'0bb5433c-11a0-480c-af64-f98898cdd070', 30, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 12, 19, 1)
GO
INSERT [dbo].[SHELF] ([Id], [Number], [ShelfTypeId], [LocationX], [LocationY], [OrientationHorizontal]) VALUES (N'867af828-011f-4fcd-88b9-fea0d18f6b8e', 78, N'bcc9f172-052f-466d-b63c-e9901a6fee7d', 3, 2, 0)
GO


INSERT [dbo].[SHELFTENANT] ([Id], [FirstName], [LastName], [Address], [PostalCode], [City], [Email], [PhoneNumber], [Status]) VALUES (N'f1e2d3c4-b5a6-4b7c-8d9e-0f1a2b3c4d5e', N'Anton', N'Mikkelsen', N'Third Street 3', N'9101', N'Odense', N'Anton@gmail.com', N'30303030', N'Inactive')
GO
INSERT [dbo].[SHELFTENANT] ([Id], [FirstName], [LastName], [Address], [PostalCode], [City], [Email], [PhoneNumber], [Status]) VALUES (N'd4a5e8f1-6c2b-4c3a-9f4e-1a2b3c4d5e6f', N'Louise', N'Ebersbach', N'Some Street 1', N'1234', N'Copenhagen', N'Louise@gmail.com', N'10101010', N'Active')
GO
INSERT [dbo].[SHELFTENANT] ([Id], [FirstName], [LastName], [Address], [PostalCode], [City], [Email], [PhoneNumber], [Status]) VALUES (N'a1b2c3d4-e5f6-4a3b-9c8d-7e6f5a4b3c2d', N'Peter', N'Holm', N'Another Street 2', N'5678', N'Aarhus', N'Peter@gmail.com', N'20202020', N'Active')
GO

