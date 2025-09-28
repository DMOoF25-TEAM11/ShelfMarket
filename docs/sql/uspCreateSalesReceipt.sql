/*
    Procedure:   uspCreateSalesReceipt
    Purpose:     Insert a new SALESRECEIPT and its lines; auto-calculate TotalAmount & VatAmount.

    VAT / Commission Business Rules (single COMPANYINFO row assumed):
      1) IsTaxUsedItem = 1 (and IsTaxRegistered = 1):
            Line UnitPrice already includes commission (commission is VAT-inclusive).
            VAT payable only on embedded VAT inside commission:
              Tax = Gross * ( c/(100+c) ) * ( (v/100)/(1+v/100) )
            Total = Gross (sum UnitPrice).
      2) IsTaxUsedItem = 0 AND IsTaxRegistered = 1:
            UnitPrice is NET (ex VAT).
            Tax = Net * (v/100).
            Total = Net + Tax.
      3) IsTaxRegistered = 0:
            No VAT (Tax = 0; Total = sum UnitPrice).

    Parameters:
      @IssuedAt (defaults GETDATE())
      @PaidByCash / @PaidByMobile  (exactly one must be 1)
      @Lines  TVP (ShelfNumber, UnitPrice)
      @ReceiptId (OUTPUT) GUID
      @ReceiptNumber (OUTPUT) identity value

    Table Type (auto-created if missing):
      dbo.SalesReceiptLineInput (ShelfNumber INT, UnitPrice DECIMAL(18,2) CHECK >= 0)

    Returns: Inserted receipt row with applied scenario & rates.

    Change Log:
      2025-09-28 Added procedure.
      2025-09-28 FIX: Replaced BIT addition (@PaidByCash + @PaidByMobile) with
                      integer coercion to avoid Msg 402 (bit+bit invalid).
*/

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE [ShelfMarket_Dev];
GO

/* Create TVP type if not already existing */
IF NOT EXISTS (SELECT 1 FROM sys.types WHERE is_table_type = 1 AND name = 'SalesReceiptLineInput')
BEGIN
    CREATE TYPE dbo.SalesReceiptLineInput AS TABLE
    (
        ShelfNumber INT NOT NULL,
        UnitPrice   DECIMAL(18,2) NOT NULL CHECK (UnitPrice >= 0)
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.uspCreateSalesReceipt
      @IssuedAt       DATETIME = NULL
    , @PaidByCash     BIT
    , @PaidByMobile   BIT
    , @Lines          dbo.SalesReceiptLineInput READONLY
    , @ReceiptId      UNIQUEIDENTIFIER OUTPUT
    , @ReceiptNumber  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @IssuedAt IS NULL
        SET @IssuedAt = GETDATE();

    /* Validate payment flags (FIX for Msg 402: avoid BIT + BIT) */
    DECLARE @CashFlag INT  = CASE WHEN @PaidByCash   = 1 THEN 1 ELSE 0 END;
    DECLARE @MobileFlag INT= CASE WHEN @PaidByMobile = 1 THEN 1 ELSE 0 END;

    IF (@PaidByCash IS NULL OR @PaidByMobile IS NULL)
    BEGIN
        RAISERROR('PaidByCash and PaidByMobile must both be provided (0 or 1).',16,1);
        RETURN;
    END

    IF (@CashFlag + @MobileFlag) <> 1
    BEGIN
        RAISERROR('Exactly one payment flag must be 1 (Cash XOR Mobile).',16,1);
        RETURN;
    END

    /* Validate lines */
    IF NOT EXISTS (SELECT 1 FROM @Lines)
    BEGIN
        RAISERROR('At least one line required.',16,1);
        RETURN;
    END

    /* Load company flags (single row) */
    DECLARE @IsTaxUsedItem BIT = 0,
            @IsTaxRegistered BIT = 0;

    SELECT TOP(1)
        @IsTaxUsedItem   = ISNULL(IsTaxUsedItem,0),
        @IsTaxRegistered = ISNULL(IsTaxRegistered,0)
    FROM dbo.COMPANYINFO
    ORDER BY Id;

    /* Effective rates at IssuedAt */
    DECLARE @CommissionRatePct DECIMAL(9,4) = 0,
            @VatRatePct        DECIMAL(9,4) = 0;

    SELECT TOP(1) @CommissionRatePct = ISNULL(RateProcent,0)
    FROM dbo.COMMISSION
    WHERE EffectiveFrom <= @IssuedAt
      AND (EffectiveTo IS NULL OR EffectiveTo >= @IssuedAt)
    ORDER BY EffectiveFrom DESC;

    SELECT TOP(1) @VatRatePct = ISNULL(RatePercent,0)
    FROM dbo.VATRATES
    WHERE EffectiveFrom <= CAST(@IssuedAt AS DATE)
      AND (EffectiveTo IS NULL OR EffectiveTo >= CAST(@IssuedAt AS DATE))
    ORDER BY EffectiveFrom DESC;

    DECLARE @VatRateFraction DECIMAL(18,10) = CASE WHEN @VatRatePct = 0 THEN 0 ELSE @VatRatePct / 100.0 END;
    DECLARE @VatFractionOnCommission DECIMAL(18,10) = 0;

    IF @IsTaxUsedItem = 1 AND @CommissionRatePct > 0 AND @VatRatePct > 0
    BEGIN
        /* (c/(100+c)) * ( (v/100)/(1+v/100) ) */
        SET @VatFractionOnCommission =
            (@CommissionRatePct / (100.0 + @CommissionRatePct)) *
            (@VatRateFraction / (1 + @VatRateFraction));
    END

    /* Aggregate incoming lines */
    DECLARE @LineSum DECIMAL(18,2);
    SELECT @LineSum = CAST(SUM(UnitPrice) AS DECIMAL(18,2)) FROM @Lines;

    DECLARE @VatAmount DECIMAL(18,2) = 0,
            @TotalAmount DECIMAL(18,2) = 0,
            @Scenario NVARCHAR(40);

    IF @IsTaxRegistered = 0
    BEGIN
        SET @Scenario = 'NoVAT_NotRegistered';
        SET @VatAmount = 0;
        SET @TotalAmount = @LineSum;
    END
    ELSE IF @IsTaxUsedItem = 1
    BEGIN
        SET @Scenario = 'UsedItem_CommissionVATOnly';
        SET @VatAmount = ROUND(@LineSum * @VatFractionOnCommission, 2);
        SET @TotalAmount = @LineSum;
    END
    ELSE
    BEGIN
        SET @Scenario = 'Normal_VAT_FullPrice';
        SET @VatAmount = ROUND(@LineSum * @VatRateFraction, 2);
        SET @TotalAmount = ROUND(@LineSum + @VatAmount, 2);
    END

    BEGIN TRAN;
        SET @ReceiptId = NEWID();

        INSERT dbo.SALESRECEIPT (Id, IssuedAt, TotalAmount, VatAmount, PaidByCash, PaidByMobile)
        VALUES (@ReceiptId, @IssuedAt, @TotalAmount, @VatAmount, @PaidByCash, @PaidByMobile);

        SELECT @ReceiptNumber = ReceiptNumber
        FROM dbo.SALESRECEIPT
        WHERE Id = @ReceiptId;

        INSERT dbo.SALESRECEIPTLINE (Id, ShelfNumber, SalesReceiptId, UnitPrice)
        SELECT NEWID(), l.ShelfNumber, @ReceiptId, l.UnitPrice
        FROM @Lines l;
    COMMIT;

    SELECT
        r.Id,
        r.ReceiptNumber,
        r.IssuedAt,
        r.TotalAmount,
        r.VatAmount,
        @Scenario AS GrossModelScenario,
        @CommissionRatePct AS CommissionRateApplied,
        @VatRatePct AS VatRateApplied,
        @LineSum AS BaseLineSum
    FROM dbo.SALESRECEIPT r
    WHERE r.Id = @ReceiptId;
END
GO

-- Quick test:
-- DECLARE @rid UNIQUEIDENTIFIER, @rno INT;
-- DECLARE @lines dbo.SalesReceiptLineInput;
-- INSERT @lines VALUES (41,125.00),(41,80.00);
-- EXEC dbo.uspCreateSalesReceipt
--   @PaidByCash=1,@PaidByMobile=0,@Lines=@lines,
--   @ReceiptId=@rid OUTPUT,@ReceiptNumber=@rno OUTPUT;
-- SELECT @rid,@rno;