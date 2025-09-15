using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class ShelfTenantContractRepository : Repository<ShelfTenantContract>, IShelfTenantContractRepository
{
    public ShelfTenantContractRepository(ShelfMarketDbContext context) : base(context)
    {
    }
}
