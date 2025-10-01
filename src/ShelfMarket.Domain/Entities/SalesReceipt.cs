namespace ShelfMarket.Domain.Entities;

public class SalesReceipt
{
    public Guid? Id { get; set; }
    public int? ReceiptNumber { get; set; }
    public DateTime IssuedAt { get; set; }
    // Vat depends on rules in Denmark, so it can be 0%, 25% of hole sale, or 25% of commissionable.
    public decimal VatAmount { get; set; }
    public bool PaidByCash { get; set; }
    public bool PaidByMobile { get; set; }

    // Navigation properties
    public virtual List<SalesReceiptLine>? SalesLine { get; set; } = new List<SalesReceiptLine>();

    public SalesReceipt()
    {

    }

    public SalesReceipt(bool paidByCash = true, bool paidByMobile = false)
    {
        IssuedAt = DateTime.Now;
        PaidByCash = paidByCash;
        PaidByMobile = paidByMobile;
    }
}
