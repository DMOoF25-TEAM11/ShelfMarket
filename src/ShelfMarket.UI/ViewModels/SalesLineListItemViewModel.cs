using ShelfMarket.Domain.Entities;

namespace ShelfMarket.UI.ViewModels;

class SalesLineListItemViewModel
{
    public SalesLineListItemViewModel(SalesLine salesLine)
    {
        Id = salesLine.Id;
        EAN = salesLine.EAN;
        ShelfNumber = salesLine.ShelfNumber;
        Price = salesLine.Price;
    }
    public Guid? Id { get; }
    public string? EAN { get; }
    public uint ShelfNumber { get; }
    public decimal Price { get; }
}
