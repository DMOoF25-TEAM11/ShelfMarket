using ShelfMarket.Application.DTOs;
using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

public interface ISalesRepository : IRepository<SalesReceipt>
{
    Task<decimal> GetCashSalesAsync(DateTime date);

    Task<SalesReceiptWithTotalAmountDto> SetSaleAsync(SalesReceipt salesRecord, IEnumerable<SalesReceiptLine> lines);
}
