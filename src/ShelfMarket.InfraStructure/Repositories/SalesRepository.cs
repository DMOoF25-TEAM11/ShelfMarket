using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class SalesRepository : Repository<Sales>, ISalesRepository
{
    public SalesRepository(ShelfMarketDbContext context) : base(context)
    {
    }
}
