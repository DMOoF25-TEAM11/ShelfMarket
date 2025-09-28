
USE [ShelfMarket_Dev]
GO

-- Treats double quotes (") as identifier delimiters (object names), not as string delimiters.
SET QUOTED_IDENTIFIER ON;
GO

/***************************************************************************************************
 Procedure: dbo.uspShelvesForShelfTenantThatTenantRent
 Description:
    Lists shelves (only ID and Number) that a specified tenant is renting on a given date.
    A shelf qualifies if it appears on at least one line of an "active" contract
    for the tenant on @AtDate.

 Active contract definition:
    c.ShelfTenantId = @ShelfTenantId
    AND c.StartDate <= @AtDate
    AND COALESCE(c.CancelledAt, c.EndDate) >= @AtDate

 Parameters:
    @ShelfTenantId UNIQUEIDENTIFIER  (Required)
    @AtDate        DATE              (Required)

 Result Set (distinct rows):
    ShelfId     UNIQUEIDENTIFIER
    ShelfNumber INT

 Error Conditions:
    - NULL @ShelfTenantId -> RAISERROR
    - NULL @AtDate        -> RAISERROR

 Usage Examples:
    DECLARE @D date = '2025-09-27';
    EXEC dbo.uspShelvesForShelfTenantThatTenantRent
         @ShelfTenantId = 'D4A5E8F1-6C2B-4C3A-9F4E-1A2B3C4D5E6F',
         @AtDate        = @D;

 Suggested Indexes:
    SHELFTENANTCONTRACT (ShelfTenantId, StartDate, EndDate, CancelledAt)
    SHELFTENANTCONTRACTLINE (ShelfTenantContractId, ShelfId)
    SHELF (Id, Number)

 Notes:
    - DISTINCT ensures a shelf with multiple contract lines (edge cases) appears once.
    - No tenant status filter; add if business rules require.
***************************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].[uspShelvesForShelfTenantThatTenantRent]
    @ShelfTenantId UNIQUEIDENTIFIER,
    @AtDate        DATE
AS
BEGIN
    SET NOCOUNT ON;

    IF @ShelfTenantId IS NULL
    BEGIN
        RAISERROR('ShelfTenantId is required.', 16, 1);
        RETURN;
    END;

    IF @AtDate IS NULL
    BEGIN
        RAISERROR('AtDate is required.', 16, 1);
        RETURN;
    END;

    ;WITH ActiveContracts AS
    (
        SELECT c.Id
        FROM dbo.SHELFTENANTCONTRACT c
        WHERE c.ShelfTenantId = @ShelfTenantId
          AND c.StartDate <= @AtDate
          AND COALESCE(c.CancelledAt, c.EndDate) >= @AtDate
    )
    SELECT DISTINCT
        s.Id     AS ShelfId,
        s.Number AS ShelfNumber
    FROM ActiveContracts ac
    INNER JOIN dbo.SHELFTENANTCONTRACTLINE cl
        ON cl.ShelfTenantContractId = ac.Id
    INNER JOIN dbo.SHELF s
        ON s.Id = cl.ShelfId
    ORDER BY s.Number;
END
GO
