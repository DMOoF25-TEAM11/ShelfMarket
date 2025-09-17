namespace ShelfMarket.Domain.Entities;

public class Shelf
{
    public Guid? Id { get; set; }
    public int Number { get; set; }
    public Guid ShelfTypeId { get; set; }
    public int LocationX { get; set; }
    public int LocationY { get; set; }
    public bool OrientationHorizontal { get; set; }

    public Shelf()
    { }

    public Shelf(int number, Guid shelfTypeId, int locationX, int locationY, bool orientationHorizontal = true)
    {
        Number = number;
        ShelfTypeId = shelfTypeId;
        LocationX = locationX;
        LocationY = locationY;
        OrientationHorizontal = orientationHorizontal;
    }
}
