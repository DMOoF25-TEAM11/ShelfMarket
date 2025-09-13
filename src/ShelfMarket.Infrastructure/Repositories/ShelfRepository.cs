using Microsoft.EntityFrameworkCore;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for <see cref="Shelf"/> entities.
/// Provides data access methods specific to shelves.
/// </summary>
public class ShelfRepository : Repository<Shelf>, IShelfRepository
{
    /// <summary>
    /// Ranges of locked locations (YStart, YEnd, XStart, XEnd).
    /// These represent areas (e.g., wall, toilet, office) where shelves cannot be placed.
    /// </summary>
    private static readonly (uint YStart, uint YEnd, uint XStart, uint XEnd)[] _lockedLocationRange = new (uint, uint, uint, uint)[]
    {
        (0, 4, 0, 19),   // Locked by wall (Y: 0-4, X: 0-19) Toilet
        (7, 19, 0, 9),   // Locked by wall (Y: 7-19, X: 0-9) Office
    };

    /// <summary>
    /// All locked locations as (Y, X) pairs, generated from <see cref="_lockedLocationRange"/>.
    /// </summary>
    private static readonly (uint Y, uint X)[] _lockedLocations = GetLockedLocations();

    /// <summary>
    /// Generates all locked (Y, X) locations from the defined ranges.
    /// </summary>
    /// <returns>An array of locked (Y, X) coordinate pairs.</returns>
    private static (uint Y, uint X)[] GetLockedLocations()
    {
        var locations = new List<(uint Y, uint X)>();
        foreach (var (yStart, yEnd, xStart, xEnd) in _lockedLocationRange)
        {
            for (uint y = yStart; y <= yEnd; y++)
            {
                for (uint x = xStart; x <= xEnd; x++)
                {
                    locations.Add((y, x));
                }
            }
        }
        return [.. locations];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShelfRepository"/> class.
    /// </summary>
    /// <param name="context">The database context to use.</param>
    public ShelfRepository(ShelfMarketDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Checks asynchronously if a shelf location is free (not occupied by any shelf and not locked).
    /// </summary>
    /// <param name="locationX">The X coordinate of the location.</param>
    /// <param name="locationY">The Y coordinate of the location.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// <c>true</c> if the location is free (no shelf exists at the given coordinates and not locked); otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> IsLocationFree(uint locationX, uint locationY, CancellationToken cancellationToken = default)
    {
        return !await _dbSet.AnyAsync(shelf => shelf.LocationX == locationX && shelf.LocationY == locationY, cancellationToken) && !await IsLocationLocked(locationX, locationY);
    }

    /// <summary>
    /// Checks asynchronously if a location is locked by wall, toilet, or office.
    /// </summary>
    /// <param name="locationX">The X coordinate of the location.</param>
    /// <param name="locationY">The Y coordinate of the location.</param>
    /// <returns>
    /// <c>true</c> if the location is locked; otherwise, <c>false</c>.
    /// </returns>
    private static Task<bool> IsLocationLocked(uint locationX, uint locationY)
    {
        // No I/O or async work needed, but keep async signature for interface compatibility
        bool isLocked = _lockedLocations.Any(loc => loc.X == locationX && loc.Y == locationY);
        return Task.FromResult(isLocked);
    }
}
