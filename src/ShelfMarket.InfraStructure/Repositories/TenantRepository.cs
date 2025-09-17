using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories
{
    public class TenantRepository : Repository<ShelfTenant>, ITenantRepository
    {
        public TenantRepository(ShelfMarketDbContext context) : base(context) { }
    }
}