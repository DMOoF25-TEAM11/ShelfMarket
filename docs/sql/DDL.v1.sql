---------------------------------------------------------
--------------             DDL             --------------
---------------------------------------------------------

-- Treats double quotes (") as identifier delimiters (object names), not as string delimiters.
SET QUOTED_IDENTIFIER ON;
GO


/*
    Drop existing database if it exists 
*/
USE master;
GO

IF DB_ID(N'ShelfMarket_Dev') IS NOT NULL
BEGIN
    ALTER DATABASE [ShelfMarket_Dev] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [ShelfMarket_Dev];
END
GO

/*
    Create new database
*/
CREATE DATABASE [ShelfMarket_Dev];
GO

/*
    Use the newly created database
*/
USE [ShelfMarket_Dev];
GO

/*
    Create schema
*/

CREATE TABLE [dbo].[SHELFTYPE] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(255) NOT NULL UNIQUE,
    [Description] NVARCHAR(200) NULL
);
Go

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

CREATE TABLE [dbo].[SHELFTENANTCONTRACTLINE] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [ShelfTenantContractId] UNIQUEIDENTIFIER NOT NULL,
    [ShelfId] UNIQUEIDENTIFIER NOT NULL,
    [LineNumber] INT NOT NULL,
    [PricePerMonth] DECIMAL(18, 2) NOT NULL,
    [PricePerMonthSpecial] DECIMAL(18, 2) NOT NULL,
    CONSTRAINT [UQ_ShelfTenantContract_LineNumber] UNIQUE ([ShelfTenantContractId], [LineNumber]),
    CONSTRAINT [FK_ShelfTenantContract] FOREIGN KEY ([ShelfTenantContractId]) REFERENCES [dbo].[SHELFTENANTCONTRACT]([Id]) ON DELETE CASCADE
);
GO