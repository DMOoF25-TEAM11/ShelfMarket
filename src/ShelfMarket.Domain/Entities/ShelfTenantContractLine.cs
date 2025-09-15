namespace ShelfMarket.Domain.Entities;

public class ShelfTenantContractLine
{
    public Guid Id { get; set; }
    public Guid ShelfTenantContractId { get; set; } = Guid.Empty;
    public Guid ShelfId { get; set; } = Guid.Empty;
    public uint LineNumber { get; set; }
    public decimal PricePerMonth { get; set; } /* Price per month in the contract from autocalculate */
    public decimal? PricePerMonthSpecial { get; set; }

    public ShelfTenantContractLine()
    {

    }

    public ShelfTenantContractLine(Guid shelfTenantContractId, Guid shelfId, uint lineNumber, decimal pricePerMonth, decimal? pricePerMonthSpecial = null)
    {
        Id = Guid.NewGuid();
        ShelfTenantContractId = shelfTenantContractId;
        ShelfId = shelfId;
        LineNumber = lineNumber;
        PricePerMonth = pricePerMonth;
        PricePerMonthSpecial = pricePerMonthSpecial;
    }

}
