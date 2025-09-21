using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract.Services;

/// <summary>
/// Application service concerned with shelf layout (grid positions and orientation).
/// </summary>
public interface IShelfLayoutService
{
    Task<IEnumerable<Shelf>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> TryUpdatePositionAsync(int shelfNumber, int locationX, int locationY, bool orientationHorizontal, CancellationToken cancellationToken = default);
    /// <summary>
    /// Tries to create a new shelf at the given position. Fails if number exists or cell is occupied.
    /// </summary>
    Task<bool> TryCreateShelfAsync(int number, bool orientationHorizontal, int locationX = 22, int locationY = 0, CancellationToken cancellationToken = default);
}


