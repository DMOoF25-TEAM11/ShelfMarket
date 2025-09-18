using ShelfMarket.Domain.ValueObjects;

namespace ShelfMarket.Domain.Entities;

public class Sale
{
    public Guid? Id { get; set; }
    public uint? Number { get; set; }
    public DateTime Date { get; set; }
    public PaymentMetod PaymentMetode { get; set; }

    public Sale()
    {

    }

    public Sale(PaymentMetod paymentMetode = PaymentMetod.Cash)
    {
        Date = DateTime.Now;
        PaymentMetode = paymentMetode;
    }
}
