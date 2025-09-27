namespace ShelfMarket.Application.DTOs;

public class ShelfTenantContractLineDto
{
    public Guid? Id { get; set; }
    public Guid ShelfTenantContractId { get; set; }
    public Guid ShelfId { get; set; }
    public int ShelfNumber { get; set; }
    public int LineNumber { get; set; }
    public decimal PricePerMonth { get; set; }
    public decimal? PricePerMonthSpecial { get; set; }
}
