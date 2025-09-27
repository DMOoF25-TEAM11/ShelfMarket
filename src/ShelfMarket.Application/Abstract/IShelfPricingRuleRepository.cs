using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

public interface IShelfPricingRuleRepository : IRepository<ShelfPricingRule>
{
    Task<IReadOnlyList<ShelfPricingRule>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetUnitPriceAsync(int shelfCount, CancellationToken cancellationToken = default);
}