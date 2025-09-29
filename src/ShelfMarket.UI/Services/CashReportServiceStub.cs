using ShelfMarket.Application.Abstract.Services;

namespace ShelfMarket.UI.Services;

/// <summary>
/// Temporary stub. Implement real logic to sum SALESRECEIPT rows where PaidByCash = 1 for the date.
/// </summary>
public class CashReportServiceStub : ICashReportService
{
    public Task<decimal> GetCashSalesAsync(DateTime date)
    {
        // Return 0 so the view still functions; inject real implementation later.
        return Task.FromResult(0m);
    }
}