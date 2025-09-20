namespace ShelfMarket.Domain.Entities;

public class SalesLine
{
    public Guid? Id { get; set; }
    public string? EAN { get; set; }
    public uint ShelfNumber { get; set; }
    public decimal Price { get; set; } = decimal.Zero;

    public SalesLine()
    {
    }

    public SalesLine(uint shelfNumber, decimal price, string? ean = null)
    {
        EAN = ean;
        ShelfNumber = shelfNumber;
        Price = price;
    }
}
