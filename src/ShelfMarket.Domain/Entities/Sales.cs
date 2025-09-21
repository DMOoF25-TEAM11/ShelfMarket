using ShelfMarket.Domain.ValueObjects;

namespace ShelfMarket.Domain.Entities;

public class Sales
{
    public Guid? Id { get; set; }
    public uint? Number { get; set; }
    public DateTime Date { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public Sales()
    {

    }

    public Sales(PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        Date = DateTime.Now;
        PaymentMethod = paymentMethod;
    }
}
