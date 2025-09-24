using Microsoft.EntityFrameworkCore;
using ShelfMarket.Application.Abstract;
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
    private static readonly (int YStart, int YEnd, int XStart, int XEnd)[] _lockedLocationRange = new[]
    {
        (0, 4, 11, 18),   // Locked by wall (Y: 0-4, X: 11-18) Toilet / Staff room
        (19, 21, 15, 18), // Locked by wall (Y: 19-21, X: 15-18) Counter
    };

    /// <summary>
    /// All locked locations as (Y, X) pairs, generated from <see cref="_lockedLocationRange"/>.
    /// </summary>
    private static readonly (int Y, int X)[] _lockedLocations = GetLockedLocations();

    /// <summary>
    /// Generates all locked (Y, X) locations from the defined ranges.
    /// </summary>
    /// <returns>An array of locked (Y, X) coordinate pairs.</returns>
    /// <returns>
    /// An array of locked (Y, X) coordinate pairs.
    /// </returns>
    private static (int Y, int X)[] GetLockedLocations()
    {
        var locations = new List<(int Y, int X)>();
        foreach (var (yStart, yEnd, xStart, xEnd) in _lockedLocationRange)
        {
            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
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
    [Obsolete("Use IsLocationFreeAsync instead.")]
    public async Task<bool> IsLocationFree(int locationX, int locationY, CancellationToken cancellationToken = default)
    {
        bool isLocationFree = true;
        List<Shelf>? AllShelfs = await _dbSet.ToListAsync(cancellationToken);
        if (AllShelfs.Any())
        {
            foreach (var shelf in AllShelfs)
            {
                // Check if the location is occupied by any shelf (either anchor or second cell)
                if ((shelf.LocationX == locationX && shelf.LocationY == locationY) ||
                    (shelf.OrientationHorizontal && shelf.LocationX + 1 == locationX && shelf.LocationY == locationY) ||
                    (!shelf.OrientationHorizontal && shelf.LocationX == locationX && shelf.LocationY + 1 == locationY))
                {
                    isLocationFree = false;
                    break;
                }
            }
            return isLocationFree && !IsLocationLocked(locationX, locationY);
        }
        // If no shelves exist, just check if location is locked
        return !IsLocationLocked(locationX, locationY);
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
    [Obsolete("Use IsLocationFreeAsync(uint x, uint y, bool orientationHorizontal, CancellationToken). This version checks only a single cell.")]
    public async Task<bool> IsLocationFreeAsync(int locationX, int locationY, CancellationToken cancellationToken = default)
    {
        bool isOccupied = await _dbSet.AnyAsync(
            shelf => shelf.LocationX == locationX && shelf.LocationY == locationY,
            cancellationToken);

        bool isLocked = IsLocationLocked(locationX, locationY);

        return !isOccupied && !isLocked;
    }

    /// <summary>
    /// Checks asynchronously if a shelf location is free, considering shelf length (2) and orientation.
    /// Assumes (locationX, locationY) is the leftmost/topmost cell of the shelf.
    /// </summary>
    /// <param name="locationX">The X coordinate of the leftmost/topmost cell.</param>
    /// <param name="locationY">The Y coordinate of the leftmost/topmost cell.</param>
    /// <param name="orientationHorizontal">True if shelf is horizontal; false if vertical.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// <c>true</c> if both cells are free (no shelf exists at the given coordinates and not locked); otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> IsLocationFreeAsync(int locationX, int locationY, bool orientationHorizontal, CancellationToken cancellationToken = default)
    {
        var second = orientationHorizontal
            ? (X: locationX + 1, Y: locationY)
            : (X: locationX, Y: locationY + 1);

        bool overlaps = await _dbSet.AnyAsync(s =>
               // Existing shelf anchors match either candidate cell
               (s.LocationX == locationX && s.LocationY == locationY)
            || (s.LocationX == second.X && s.LocationY == second.Y)
            // Existing shelf second cell matches either candidate cell
            || (s.OrientationHorizontal && (
                   (s.LocationX + 1 == locationX && s.LocationY == locationY)
                || (s.LocationX + 1 == second.X && s.LocationY == second.Y)))
            || (!s.OrientationHorizontal && (
                   (s.LocationX == locationX && s.LocationY + 1 == locationY)
                || (s.LocationX == second.X && s.LocationY + 1 == second.Y))),
            cancellationToken);

        if (overlaps)
            return false;

        return !IsLocationLocked(locationX, locationY) && !IsLocationLocked(second.X, second.Y);
    }

    /// <summary>
    /// Checks if a location is locked by wall, toilet, or office.
    /// </summary>
    /// <param name="locationX">The X coordinate of the location.</param>
    /// <param name="locationY">The Y coordinate of the location.</param>
    /// <returns>
    /// <c>true</c> if the location is locked; otherwise, <c>false</c>.
    /// </returns>
    private static bool IsLocationLocked(int locationX, int locationY) =>
        _lockedLocations.Any(loc => loc.X == locationX && loc.Y == locationY);
}
