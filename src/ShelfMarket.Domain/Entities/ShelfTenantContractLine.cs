namespace ShelfMarket.Domain.Entities;

public class ShelfTenantContractLine
{
    public Guid? Id { get; set; }
    public Guid ShelfTenantContractId { get; set; } = Guid.Empty;
    public Guid ShelfId { get; set; } = Guid.Empty;
    public int LineNumber { get; set; }
    public decimal PricePerMonth { get; set; } /* Price per month in the contract from autocalculate */
    public decimal? PricePerMonthSpecial { get; set; }

    public ShelfTenantContractLine()
    {

    }

    public ShelfTenantContractLine(Guid shelfTenantContractId, Guid shelfId, decimal pricePerMonth, decimal? pricePerMonthSpecial = null)
    {
        ShelfTenantContractId = shelfTenantContractId;
        ShelfId = shelfId;
        PricePerMonth = pricePerMonth;
        PricePerMonthSpecial = pricePerMonthSpecial;
    }

}
