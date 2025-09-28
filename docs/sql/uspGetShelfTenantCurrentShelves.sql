USE [ShelfMarket_Dev]
GO

-- Treats double quotes (") as identifier delimiters (object names), not as string delimiters.
SET QUOTED_IDENTIFIER ON;
GO

/***************************************************************************************************
 Procedure: dbo.uspGetShelfTenantCurrentShelves
 Description:
    Returns (for "today") the shelves currently assigned to shelf tenants whose contracts
    are active on the current date. Optionally filters by tenant email.

 Active contract criteria:
    StartDate <= Today
    AND EndDate >= Today
    AND (CancelledAt IS NULL OR CancelledAt > Today)

 Parameters:
    @Email NVARCHAR(255) = NULL
        - If NULL  : Include all tenants.
        - If NOT NULL: Filter to the tenant with the exact email.

 Result Set (one row per active contract line / shelf):
    ShelfNumber (INT)
        The shelf's business number (not the GUID Id). Uncomment additional columns if needed.

 Notes:
    - Additional tenant / contract / shelf columns are present as commented lines for easy expansion.
    - Adjust the optional status filter if only "Active" tenants should be included.

 Usage Examples:
    EXEC dbo.uspGetShelfTenantCurrentShelves @Email = 'tenant@example.com';
    EXEC dbo.uspGetShelfTenantCurrentShelves @Email = NULL;

 Performance Considerations:
    - Ensure indexes on:
        SHELFTENANTCONTRACT (ShelfTenantId, StartDate, EndDate, CancelledAt)
        SHELFTENANTCONTRACTLINE (ShelfTenantContractId, ShelfId)
        SHELF (Id, Number)
        SHELFTENANT (Email)
***************************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].[uspGetShelfTenantCurrentShelves]
    @Email NVARCHAR(255) = NULL  -- pass NULL to get all tenants
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today date = CONVERT(date, GETDATE());

    SELECT
        --s.Id                    AS ShelfId,
        s.Number                AS ShelfNumber
    FROM dbo.SHELFTENANT                AS t
    JOIN dbo.SHELFTENANTCONTRACT        AS c  ON c.ShelfTenantId = t.Id
    JOIN dbo.SHELFTENANTCONTRACTLINE    AS cl ON cl.ShelfTenantContractId = c.Id
    JOIN dbo.SHELF                      AS s  ON s.Id = cl.ShelfId
    WHERE
        (@Email IS NULL OR t.Email = @Email)
        AND (@Today BETWEEN CONVERT(date, c.StartDate) AND CONVERT(date, c.EndDate))
        AND (c.CancelledAt IS NULL OR @Today < CONVERT(date, c.CancelledAt))
        -- AND t.Status = 'Active'   -- Uncomment if you only want active status tenants
    ORDER BY
        s.Number;
END
GO