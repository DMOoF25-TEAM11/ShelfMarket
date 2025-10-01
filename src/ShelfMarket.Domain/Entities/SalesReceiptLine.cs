namespace ShelfMarket.Domain.Entities;

public class SalesReceiptLine
{
    public Guid? Id { get; set; }
    public Guid? SalesReceiptId { get; set; }
    public int ShelfNumber { get; set; }
    public decimal UnitPrice { get; set; }

    public SalesReceiptLine()
    {
    }

    public SalesReceiptLine(int shelfNumber, decimal price)
    {
        ShelfNumber = shelfNumber;
        UnitPrice = price;
    }
}
