using ShelfMarket.Domain.Entities;

namespace ShelfMarket.UI.ViewModels;

public class ShelfTypeListItemViewModel
{
    public Guid? Id { get; }
    public string Name { get; }
    public string Description { get; }

    public ShelfTypeListItemViewModel(ShelfType shelfType)
    {
        Id = shelfType.Id;
        Name = shelfType.Name;
        Description = shelfType.Description;
    }
}
