SET QUOTED_IDENTIFIER ON;
GO
USE [ShelfMarket_Dev];
GO

/***************************************************************************************************
 Procedure: dbo.uspGetAvailableShelves
 Description:
    Returns shelves that have no overlapping assignment (any contract line) within the supplied
    date range [@StartDate .. @EndDate].

 Availability logic:
    A shelf is considered "busy" if any contract line belongs to a contract whose active
    window intersects the supplied range:
        Contract.StartDate <= @EndDate
        AND COALESCE(CancelledAt, EndDate) >= @StartDate

 Parameters:
    @StartDate DATE (NOT NULL)
    @EndDate   DATE (NOT NULL)
        Must satisfy: @EndDate >= @StartDate.

 Result Set:
    ShelfId     UNIQUEIDENTIFIER
    shelfNumber INT

 Error Conditions:
    - Missing dates -> RAISERROR (severity 16).
    - @EndDate < @StartDate -> RAISERROR (severity 16).

 Usage Examples:
    EXEC dbo.uspGetAvailableShelves @StartDate = '2025-09-01', @EndDate = '2025-09-30';
    EXEC dbo.uspGetAvailableShelves @StartDate = '2025-09-15', @EndDate = '2025-09-15';

 Index Recommendations:
    SHELFTENANTCONTRACT (StartDate, EndDate, CancelledAt)
    SHELFTENANTCONTRACTLINE (ShelfTenantContractId, ShelfId)
    SHELF (Id, Number)

 Notes:
    - Using a CTE (OverlappingContracts) isolates candidate contract IDs before resolving shelf usage.
***************************************************************************************************/

CREATE OR ALTER PROCEDURE [dbo].[uspGetAvailableShelves]
    @StartDate date,
    @EndDate   date
AS
BEGIN
    SET NOCOUNT ON;

    IF @StartDate IS NULL OR @EndDate IS NULL
    BEGIN
        RAISERROR('StartDate and EndDate are required.', 16, 1);
        RETURN;
    END;

    IF @EndDate < @StartDate
    BEGIN
        RAISERROR('EndDate must be on or after StartDate.', 16, 1);
        RETURN;
    END;

    ;WITH OverlappingContracts AS
    (
        SELECT c.Id
        FROM dbo.SHELFTENANTCONTRACT AS c
        WHERE c.StartDate <= @EndDate
          AND COALESCE(c.CancelledAt, c.EndDate) >= @StartDate
    ),
    BusyShelves AS
    (
        SELECT DISTINCT l.ShelfId
        FROM dbo.SHELFTENANTCONTRACTLINE AS l
        INNER JOIN OverlappingContracts oc
            ON oc.Id = l.ShelfTenantContractId
    )
    SELECT
        s.Id     AS ShelfId,
        s.Number AS shelfNumber
    FROM dbo.SHELF AS s
    WHERE NOT EXISTS (SELECT 1 FROM BusyShelves b WHERE b.ShelfId = s.Id)
    ORDER BY s.Number;
END
GO
