namespace ShelfMarket.Application.Abstract.Services;

/// <summary>
/// Abstraction to obtain system-expected cash sales for a given date (sum of cash receipts).
/// Replace the stub implementation with real data access (e.g. via repository / EF).
/// </summary>
public interface ICashReportService
{
    Task<decimal> GetCashSalesAsync(DateTime date);
}