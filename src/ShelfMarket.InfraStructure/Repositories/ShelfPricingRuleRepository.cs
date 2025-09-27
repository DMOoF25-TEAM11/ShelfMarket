using Microsoft.EntityFrameworkCore;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class ShelfPricingRuleRepository : Repository<ShelfPricingRule>, IShelfPricingRuleRepository
{
    public ShelfPricingRuleRepository(ShelfMarketDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ShelfPricingRule>> GetAllOrderedAsync(CancellationToken cancellationToken = default) =>
        await _dbSet.AsNoTracking()
            .OrderBy(r => r.MinShelvesInclusive)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetUnitPriceAsync(int shelfCount, CancellationToken cancellationToken = default)
    {
        if (shelfCount <= 0) return 0m;

        var rule = await _dbSet.AsNoTracking()
            .Where(r => r.MinShelvesInclusive <= shelfCount)
            .OrderBy(r => r.MinShelvesInclusive)
            .LastOrDefaultAsync(cancellationToken);

        return rule?.PricePerShelf ?? 0m;
    }
}