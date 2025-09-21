namespace ShelfMarket.Domain.Entities;

public class Ean
{
    public Guid? Id { get; set; }
    public string? Code { get; set; }
    public Guid ShelfNumber { get; set; }
    public decimal Price { get; set; } = decimal.Zero;

    public Ean()
    {
    }

    public Ean(Guid shelfNumber, decimal price, string? code = null)
    {
        Code = code;
        ShelfNumber = shelfNumber;
        Price = price;
    }
}
