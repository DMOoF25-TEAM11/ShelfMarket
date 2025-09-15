-- Create database if it does not exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ShelfMarket_Dev')
BEGIN
    CREATE DATABASE [ShelfMarket_Dev];
END
GO

-- Use the database
USE [ShelfMarket_Dev];
GO

-- Create table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SHELFTYPE')
BEGIN
    CREATE TABLE dbo.SHELFTYPE (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL UNIQUE,
        Description NVARCHAR(200) NULL
    );
END
GO

-- Create SHELF table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SHELF')
BEGIN
    CREATE TABLE dbo.SHELF (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Number INT NOT NULL UNIQUE,
        ShelfTypeId UNIQUEIDENTIFIER NOT NULL,
        LocationX INT NOT NULL,
        LocationY INT NOT NULL,
        CONSTRAINT FK_SHELF_SHELFTTYPE FOREIGN KEY (ShelfTypeId)
            REFERENCES dbo.SHELFTYPE(Id),
            UNIQUE (LocationX, LocationY)

    );
END
GO

-- Create table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SHELFRENTCONTRACT' AND TABLE_SCHEMA = N'dbo')
BEGIN
    CREATE TABLE dbo.SHELFRENTCONTRACT (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ShelfTenantId UNIQUEIDENTIFIER NOT NULL,
        ContractNumber INT NOT NULL UNIQUE,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        Cancelled DATE NULL
    );
END
GO

-- Create table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SHELFRENTCONTRACTLINE' AND TABLE_SCHEMA = N'dbo')
BEGIN
    CREATE TABLE dbo.SHELFRENTCONTRACTLINE (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ShelfRentContractId UNIQUEIDENTIFIER NOT NULL,
        LineNumber INT NOT NULL,
        Description NVARCHAR(255) NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        CONSTRAINT UQ_ShelfRentContract_LineNumber UNIQUE (ShelfRentContractId, LineNumber),
        CONSTRAINT FK_ShelfRentContract FOREIGN KEY (ShelfRentContractId) REFERENCES dbo.SHELFRENTCONTRACT(Id)
    );
END
GO


-- Use the target database
USE [ShelfMarket_Dev];

-- Insert '6 hylder' if it does not already exist
IF NOT EXISTS (SELECT 1 FROM dbo.SHELFTYPE WHERE Name = N'6 hylder')
BEGIN
    INSERT INTO dbo.SHELFTYPE (Id, Name, Description)
    VALUES (CAST('BCC9F172-052F-466D-B63C-E9901A6FEE7D' AS UNIQUEIDENTIFIER), N'6 hylder', N'Standard reol');
END

-- Insert '3 hylder og en stang' if it does not already exist
IF NOT EXISTS (SELECT 1 FROM dbo.SHELFTYPE WHERE Name = N'3 hylder og en stang')
BEGIN
    INSERT INTO dbo.SHELFTYPE (Id, Name, Description)
    VALUES (CAST('4AEBD9CF-9CD7-4F6E-BB38-C79BF279334B' AS UNIQUEIDENTIFIER), N'3 hylder og en stang', N'Egnet til tøj');
END