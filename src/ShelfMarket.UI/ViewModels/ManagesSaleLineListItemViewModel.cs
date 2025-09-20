using ShelfMarket.Domain.Entities;

namespace ShelfMarket.UI.ViewModels;

public class ManagesSaleLineListItemViewModel
{
    public Guid? Id { get; }
    public string? EAN { get; }
    public uint ReolNummer { get; }
    public decimal Pris { get; } = decimal.Zero;

    public ManagesSaleLineListItemViewModel(SalesLine salesLine)
    {
        Id = salesLine.Id;
        EAN = salesLine.EAN;
        ReolNummer = salesLine.ShelfNumber;
        Pris = salesLine.Price;
    }
}
