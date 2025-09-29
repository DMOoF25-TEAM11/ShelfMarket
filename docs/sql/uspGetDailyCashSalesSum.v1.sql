USE [ShelfMarket_Dev];
GO
CREATE OR ALTER PROCEDURE dbo.uspGetDailyCashSalesSum
    @SalesDate DATE
AS
BEGIN
    SET NOCOUNT ON;

    /* Returns sum of cash receipt line totals for the given calendar date.
       Uses a half‑open range to stay SARGable for IX_SalesReceip_IssuedAt (IssuedAt, ReceiptNumber). */
    SELECT CashSalesTotal = ISNULL(SUM(srl.UnitPrice), 0.00)
    FROM dbo.SALESRECEIPT sr
    INNER JOIN dbo.SALESRECEIPTLINE srl ON srl.SalesReceiptId = sr.Id
    WHERE sr.PaidByCash = 1
      AND sr.IssuedAt >= @SalesDate
      AND sr.IssuedAt < DATEADD(DAY, 1, @SalesDate);
END
GO
-- Example:
-- EXEC dbo.uspGetDailyCashSalesSum @SalesDate = '2025-09-29';