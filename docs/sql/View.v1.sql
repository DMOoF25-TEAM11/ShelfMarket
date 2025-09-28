---------------------------------------------------------
--------------             VIEW            --------------
---------------------------------------------------------

USE [ShelfMarket_Dev];
GO
SET QUOTED_IDENTIFIER ON;
GO

/***************************************************************************************************
  VIEW: vSalesReceiptCalculated
  FIX:
    - Removed GROUP BY error by moving line aggregation into an OUTER APPLY (t).
    - Commission & VAT rates also come from OUTER APPLY blocks; no grouping needed now.
  Calculation:
    CommissionFraction = c / (100 + c)
    VatFraction        = (v/100) / (1 + v/100)
    TaxAmount          = Gross * CommissionFraction * VatFraction
    Null-safe (missing rates => 0 tax).
***************************************************************************************************/
CREATE OR ALTER VIEW dbo.vSalesReceiptCalculated
AS
SELECT
    r.Id,
    r.ReceiptNumber,
    r.IssuedAt,
    TotalAmount = ISNULL(t.Gross, 0),
    TaxAmount   = CAST(
                   ISNULL(t.Gross,0) *
                   (CASE 
                        WHEN cc.RateProcent IS NULL OR cc.RateProcent = 0 THEN 0
                        ELSE cc.RateProcent / (100.0 + cc.RateProcent)
                    END) *
                   (CASE 
                        WHEN vv.RatePercent IS NULL OR vv.RatePercent = 0 THEN 0
                        ELSE ( (vv.RatePercent/100.0) / (1 + (vv.RatePercent/100.0)) )
                    END)
                 AS DECIMAL(18,2)),
    r.PaidByCash,
    r.PaidByMobile
FROM dbo.SALESRECEIPT r
OUTER APPLY (
    SELECT Gross = SUM(l.UnitPrice)
    FROM dbo.SALESRECEIPTLINE l
    WHERE l.SalesReceiptId = r.Id
) t
OUTER APPLY (
    SELECT TOP (1) RateProcent
    FROM dbo.COMMISSION c
    WHERE c.EffectiveFrom <= r.IssuedAt
      AND (c.EffectiveTo IS NULL OR c.EffectiveTo >= r.IssuedAt)
    ORDER BY c.EffectiveFrom DESC
) cc
OUTER APPLY (
    SELECT TOP (1) RatePercent
    FROM dbo.VATRATES v
    WHERE v.EffectiveFrom <= CAST(r.IssuedAt AS date)
      AND (v.EffectiveTo IS NULL OR v.EffectiveTo >= CAST(r.IssuedAt AS date))
    ORDER BY v.EffectiveFrom DESC
) vv;
GO
