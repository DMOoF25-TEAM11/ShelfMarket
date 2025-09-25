using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories
{
    public class ShelfTenantRepository : Repository<ShelfTenant>, IShelfTenantRepository
    {
        public ShelfTenantRepository(ShelfMarketDbContext context) : base(context) { }
    }
}