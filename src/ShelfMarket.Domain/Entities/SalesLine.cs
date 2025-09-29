namespace ShelfMarket.Domain.Entities;

public class SalesLine
{
    public Guid? Id { get; set; }
    public Guid? SalesReceiptId { get; set; }
    public uint ShelfNumber { get; set; }
    public decimal Price { get; set; } = decimal.Zero;

    public SalesLine()
    {
    }

    public SalesLine(uint shelfNumber, decimal price)
    {
        ShelfNumber = shelfNumber;
        Price = price;
    }
}
