-- Create database if it does not exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ShelfMarked_dev')
BEGIN
    CREATE DATABASE ShelfMarked_Dev;
END
GO

-- Use the database
USE ShelfMarked_Dev;
GO

-- Create table if it does not exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SHELFTYPE')
BEGIN
    CREATE TABLE SHELFTYPE (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        Description NVARCHAR(200) NULL
    );
END
GO