using ShelfMarket.Domain.Entities;

namespace ShelfMarket.UI.ViewModels;

class SalesLineListItemViewModel
{
    public SalesLineListItemViewModel(SalesLine salesLine)
    {
        Id = salesLine.Id;
        EAN = salesLine.EAN;
        ReolNummer = salesLine.ShelfNumber;
        Pris = salesLine.Price;
    }
    public Guid? Id { get; }
    public string? EAN { get; }
    public uint ReolNummer { get; }
    public decimal Pris { get; }
}
