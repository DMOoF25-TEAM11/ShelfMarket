using ShelfMarket.Domain.Enums;

namespace ShelfMarket.Domain.Entities;

public class Sales
{
    public Guid? Id { get; set; }
    public uint? ReceiptNumber { get; set; }
    public DateTime IssuedAt { get; set; }
    public decimal VatAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public Sales()
    {

    }

    public Sales(PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        IssuedAt = DateTime.Now;
        PaymentMethod = paymentMethod;
    }
}
