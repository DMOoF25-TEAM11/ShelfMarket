using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

/// <summary>
/// Repository abstraction for managing and querying <see cref="ShelfPricingRule"/> entities
/// that define tiered (volume) pricing based on total shelf count.
/// </summary>
/// <remarks>
/// Pricing Model:
/// A collection of rules, each with a <see cref="ShelfPricingRule.MinShelvesInclusive"/> threshold and
/// a corresponding <see cref="ShelfPricingRule.PricePerShelf"/>. To determine the effective unit
/// price for a given shelf count, select the rule with the greatest <see cref="ShelfPricingRule.MinShelvesInclusive"/>
/// value that is less than or equal to the requested count (classic tier breakpoint selection).
/// 
/// Implementations:
/// - Should ensure returned ordered sequences are stable and deterministic (e.g. sorted ascending by MinShelvesInclusive).
/// - May cache results for read-heavy workloads.
/// - Should treat missing matching rule (e.g. empty rules set) consistently (commonly returning 0 or throwing
///   depending on domain requirements; this interface assumes the implementation will document such behavior).
/// </remarks>
public interface IShelfPricingRuleRepository : IRepository<ShelfPricingRule>
{
    /// <summary>
    /// Retrieves all pricing rules ordered by ascending <see cref="ShelfPricingRule.MinShelvesInclusive"/>.
    /// </summary>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A read-only list of all rules, sorted so that earlier entries represent lower tier thresholds.
    /// </returns>
    Task<IReadOnlyList<ShelfPricingRule>> GetAllOrderedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines the effective unit price for the specified <paramref name="shelfCount"/> by selecting
    /// the rule with the highest <see cref="ShelfPricingRule.MinShelvesInclusive"/> less than or equal
    /// to the count.
    /// </summary>
    /// <param name="shelfCount">Total number of shelves for which pricing is being evaluated.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// The unit price (price per shelf) applicable to the provided <paramref name="shelfCount"/>.
    /// </returns>
    /// <remarks>
    /// Implementations should define behavior when no rules exist (e.g. return 0m or throw). A common
    /// approach is to return 0m to indicate absence of configured pricing.
    /// </remarks>
    Task<decimal> GetUnitPriceAsync(int shelfCount, CancellationToken cancellationToken = default);
}