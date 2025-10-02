CREATE OR ALTER PROCEDURE dbo.uspGetOccupiedShelvesForMonth
    @MonthStart date,
    @MonthEnd   date
AS
BEGIN
    SET NOCOUNT ON;

    /*
        Returns the distinct shelf numbers that have an active (not cancelled) contract
        overlapping the requested month window. The caller should pass @MonthStart as the
        first day of the month and @MonthEnd as the first day of the following month.
    */
    SELECT DISTINCT
        s.Number AS ShelfNumber
    FROM dbo.ShelfTenantContractLine AS l
    INNER JOIN dbo.ShelfTenantContract AS c
        ON c.Id = l.ShelfTenantContractId
    INNER JOIN dbo.Shelf AS s
        ON s.Id = l.ShelfId
    WHERE c.CancelledAt IS NULL
      AND c.StartDate < @MonthEnd
      AND c.EndDate >= @MonthStart;
END;
