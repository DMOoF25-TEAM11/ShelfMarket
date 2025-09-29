using ShelfMarket.Application.DTOs;
using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

public interface ISalesRepository : IRepository<Sales>
{
    Task<decimal> GetCashSalesAsync(DateTime date);

    Task<SalesReceiptWithTotalAmountDto> SetSaleAsync(IEnumerable<SalesLine> lines, bool paidByCash, bool paidByMobile);
}
