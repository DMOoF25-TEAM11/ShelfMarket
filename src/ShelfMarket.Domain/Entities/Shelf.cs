namespace ShelfMarket.Domain.Entities;

public class Shelf
{
    public Guid? Id { get; set; }
    public int Number { get; set; }
    public Guid ShelfTypeId { get; set; }

    public Shelf()
    { }

    public Shelf(int number, Guid shelfTypeId)
    {
        Number = number;
        ShelfTypeId = shelfTypeId;
    }
}
