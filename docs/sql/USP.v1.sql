---------------------------------------------------------
--------------             USP             --------------
---------------------------------------------------------
USE [ShelfMarket_Dev]
GO

-- Treats double quotes (") as identifier delimiters (object names), not as string delimiters.
SET QUOTED_IDENTIFIER ON;
GO

/*
    Stored Procedure: uspGetShelfTenantCurrentShelves
    Description: Returns shelf tenants with active shelf assignments.
*/
CREATE OR ALTER PROCEDURE [dbo].[uspGetShelfTenantCurrentShelves]
    @Email NVARCHAR(255) = NULL  -- pass NULL to get all tenants
AS
BEGIN
    SET NOCOUNT ON;

    /*
        Returns one row per active shelf assignment (today within contract dates, not cancelled).
        Tables expected (per EF DbContext mappings):
          - dbo.SHELFTENANT (Id, FirstName, LastName, Email, PhoneNumber, Status, ...)
          - dbo.SHELFTENANTCONTRACT (Id, ShelfTenantId, ContractNumber, StartDate, EndDate, CancelledAt)
          - dbo.SHELFTENANTCONTRACTLINE (Id, ShelfTenantContractId, ShelfId, LineNumber, ...)
          - dbo.SHELF (Id, Number, LocationX, LocationY, OrientationHorizontal, ...)
    */

    DECLARE @Today date = CONVERT(date, GETDATE());

    SELECT
        --t.Id                    AS TenantId,
        --t.FirstName,
        --t.LastName,
        --t.Email,
        --t.PhoneNumber,
        --t.Status                AS TenantStatus,

        --c.Id                    AS ContractId,
        --c.ContractNumber,
        --CONVERT(date, c.StartDate) AS ContractStartDate,
        --CONVERT(date, c.EndDate)   AS ContractEndDate,

        --s.Id                    AS ShelfId,
        --s.LocationX,
        --s.LocationY,
        --s.OrientationHorizontal,
        s.Number                AS ShelfNumber
    FROM dbo.SHELFTENANT                AS t
    JOIN dbo.SHELFTENANTCONTRACT          AS c  ON c.ShelfTenantId = t.Id
    JOIN dbo.SHELFTENANTCONTRACTLINE    AS cl ON cl.ShelfTenantContractId = c.Id
    JOIN dbo.SHELF                      AS s  ON s.Id = cl.ShelfId
    WHERE
        (@Email IS NULL OR t.Email = @Email)
        AND (@Today BETWEEN CONVERT(date, c.StartDate) AND CONVERT(date, c.EndDate))
        AND (c.CancelledAt IS NULL OR @Today < CONVERT(date, c.CancelledAt))
        -- Optional: Only active tenants
        -- AND t.Status = 'Active'
    ORDER BY
        s.Number;
END
GO

-- Example execution:
-- EXEC dbo.uspGetShelfTenantCurrentShelves @Email = 'Louise@gmail.com';
-- EXEC dbo.uspGetShelfTenantCurrentShelves @Email = NULL;  -- all tenants