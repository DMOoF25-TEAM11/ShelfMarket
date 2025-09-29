using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

public interface ISalesRepository : IRepository<Sales>
{
    Task<decimal> GetCashSalesAsync(DateTime date);
}
