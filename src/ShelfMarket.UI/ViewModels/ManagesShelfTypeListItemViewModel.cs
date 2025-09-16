using ShelfMarket.Domain.Entities;

namespace ShelfMarket.UI.ViewModels;

public class ManagesShelfTypeListItemViewModel
{
    public Guid? Id { get; }
    public string Name { get; }
    public string Description { get; }

    public ManagesShelfTypeListItemViewModel(ShelfType shelfType)
    {
        Id = shelfType.Id;
        Name = shelfType.Name;
        Description = shelfType.Description;
    }
}
