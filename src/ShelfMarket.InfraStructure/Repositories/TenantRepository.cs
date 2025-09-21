using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories
{
    public class TenantRepository : Repository<ShelfTenant>, IShelfTenantRepository
    {
        public TenantRepository(ShelfMarketDbContext context) : base(context) { }
    }
}