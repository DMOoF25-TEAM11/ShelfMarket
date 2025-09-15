namespace ShelfMarket.Domain.Entities;

public class ShelfTenantContractLine
{
    public Guid Id { get; set; }
    public Guid ShelfTenantContractId { get; set; } = Guid.Empty;
    public Guid ShelfId { get; set; } = Guid.Empty;
    public uint lineNumber { get; set; }
    public decimal PricePerMonth { get; set; }
    public decimal? PricePerMonthSpecial { get; set; }

}
