using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class ShelfTenantContractLineRepository : Repository<ShelfTenantContractLine>, IShelfTenantContractLineRepository
{
    public ShelfTenantContractLineRepository(ShelfMarketDbContext context) : base(context)
    {
    }
}
