namespace ShelfMarket.Domain.Entities;

public class ShelfType
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public ShelfType()
    {
    }

    public ShelfType(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
