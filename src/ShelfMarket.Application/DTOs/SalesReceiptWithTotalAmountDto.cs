namespace ShelfMarket.Application.DTOs;

public class SalesReceiptWithTotalAmountDto
{
    public Guid? Id { get; set; }
    public uint? ReceiptNumber { get; set; }
    public DateTime IssuedAt { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public bool? PaidByCash { get; set; }
    public bool? PaidByMobile { get; set; }
}
