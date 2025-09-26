using System.Collections.ObjectModel;

namespace ShelfMarket.Domain.Entities;

// Tiered pricing based on total shelves on a contract.
// No MaxShelves; select the rule with the highest MinShelvesInclusive <= count.
public class ShelfPricingRule
{
    public Guid? Id { get; set; }
    public int MinShelvesInclusive { get; set; }
    public decimal PricePerShelf { get; set; }

    // Helper for in-memory evaluation if needed
    public static decimal GetUnitPrice(int shelfCount, IEnumerable<ShelfPricingRule> rules)
    {
        if (shelfCount <= 0) return 0m;
        var rule = rules
            .OrderBy(r => r.MinShelvesInclusive)
            .LastOrDefault(r => r.MinShelvesInclusive <= shelfCount);
        return rule?.PricePerShelf ?? 0m;
    }
}

public static class DefaultShelfPricingRules
{
    // 1 shelf -> 850, 2-3 -> 825, 4+ -> 800
    public static readonly ReadOnlyCollection<ShelfPricingRule> Rules =
        new(new[]
        {
            new ShelfPricingRule { MinShelvesInclusive = 1, PricePerShelf = 850m },
            new ShelfPricingRule { MinShelvesInclusive = 2, PricePerShelf = 825m },
            new ShelfPricingRule { MinShelvesInclusive = 4, PricePerShelf = 800m },
        });

    public static decimal GetUnitPrice(int shelfCount)
    {
        if (shelfCount < 1) return 0m;
        return Rules.First(r => r.MinShelvesInclusive <= shelfCount).PricePerShelf;
    }
}