IF OBJECT_ID(N'dbo.uspGetDailyCashSalesSum', N'P') IS NOT NULL
    DROP PROCEDURE dbo.uspGetDailyCashSalesSum;
GO
CREATE PROCEDURE dbo.uspGetDailyCashSalesSum
    @SalesDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    /* Returns sum of cash receipt totals for the given calendar date.
       Uses a half‑open range to stay SARGable for IX_SalesReceip_IssuedAt (IssuedAt, ReceiptNumber). */
    SELECT CashSalesTotal = ISNULL(SUM(sr.TotalAmount), 0.00)
    FROM dbo.SALESRECEIPT sr
    WHERE sr.PaidByCash = 1
      AND sr.IssuedAt >= @SalesDate
      AND sr.IssuedAt < DATEADD(DAY, 1, @SalesDate);
END
GO
-- Example:
-- EXEC dbo.uspGetDailyCashSalesSum @SalesDate = '2025-09-29';