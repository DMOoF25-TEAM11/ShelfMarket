using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class ShelfTypeRepository : Repository<ShelfType>, IShelfTypeRepository
{
    public ShelfTypeRepository(ShelfMarketDbContext context) : base(context)
    {
    }
}
