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