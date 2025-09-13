namespace ShelfMarket.Domain.Entities;

public class Shelf
{
    public Guid? Id { get; set; }
    public uint Number { get; set; }
    public Guid ShelfTypeId { get; set; }
    public uint LocationX { get; set; }
    public uint LocationY { get; set; }

    public Shelf()
    { }

    public Shelf(uint number, Guid shelfTypeId, uint locationX, uint locationY)
    {
        Number = number;
        ShelfTypeId = shelfTypeId;
        LocationX = locationX;
        LocationY = locationY;
    }
}
