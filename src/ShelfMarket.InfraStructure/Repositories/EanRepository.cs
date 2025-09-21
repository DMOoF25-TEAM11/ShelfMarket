using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class EanRepository : Repository<Ean>, IEanRepository
{
    public EanRepository(ShelfMarketDbContext context) : base(context)
    {
    }
}
