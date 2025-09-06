using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class ShelfRepository : Repository<Shelf>, IShelfRepository
{
    public ShelfRepository(ShelfMarketDbContext context) : base(context)
    {
    }
}
