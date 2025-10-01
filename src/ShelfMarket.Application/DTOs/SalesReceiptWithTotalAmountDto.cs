using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.DTOs;

public class SalesReceiptWithTotalAmountDto
{
    public Guid? Id { get; set; }
    public int? ReceiptNumber { get; set; }
    public DateTime IssuedAt { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public bool? PaidByCash { get; set; }
    public bool? PaidByMobile { get; set; }

    // Navigation property
    public virtual IEnumerable<SalesReceiptLine> SalesLines { get; set; } = new List<SalesReceiptLine>();
}
