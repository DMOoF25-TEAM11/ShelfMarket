using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class SalesLineRepository : Repository<SalesLine>, ISalesLineRepository
{
    public SalesLineRepository(ShelfMarketDbContext context) : base(context)
    {
    }
}
