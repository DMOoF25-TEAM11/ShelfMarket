using System.Data;
using Microsoft.Data.SqlClient; // added
using Microsoft.EntityFrameworkCore;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Application.DTOs;
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

    public async Task<IEnumerable<AvailableShelfDto>> GetAvailableShelves(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var start = startDate.Date;
        var end = endDate.Date;

        var shelves = new List<AvailableShelfDto>();

        // Use a dedicated connection to avoid sharing EF's active connection (no MARS required)
        var connString = _context.Database.GetConnectionString();
        await using var connection = new SqlConnection(connString);
        await using var command = new SqlCommand("dbo.uspGetAvailableShelves", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date) { Value = start });
        command.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.Date) { Value = end });

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            shelves.Add(new AvailableShelfDto
            {
                Id = reader.GetGuid(0),
                Number = reader.GetInt32(1)
            });
        }

        return shelves.Count == 0 ? Array.Empty<AvailableShelfDto>() : shelves;
    }
    public async Task<IEnumerable<AvailableShelfDto>> GetAvailableShelvesForTenantAsync(Guid tenantId, DateTime atDate, CancellationToken cancellationToken = default)
    {
        var date = atDate.Date;
        var shelves = new List<AvailableShelfDto>();

        var connString = _context.Database.GetConnectionString();
        await using var connection = new SqlConnection(connString);
        // Existing stored procedure name is: uspShelvesForShelfTenantThatTenantRent
        await using var command = new SqlCommand("dbo.uspShelvesForShelfTenantThatTenantRent", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        // Match stored procedure parameter names and types
        command.Parameters.Add(new SqlParameter("@ShelfTenantId", SqlDbType.UniqueIdentifier) { Value = tenantId });
        command.Parameters.Add(new SqlParameter("@AtDate", SqlDbType.Date) { Value = date });

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            shelves.Add(new AvailableShelfDto
            {
                Id = reader.GetGuid(reader.GetOrdinal("ShelfId")),
                Number = reader.GetInt32(reader.GetOrdinal("ShelfNumber"))
            });
        }
        return shelves.Count == 0 ? Array.Empty<AvailableShelfDto>() : shelves;
    }
    public async Task<bool> ExistsShelfNumberAsync(int shelfNumber, CancellationToken cancellationToken = default) =>
        await _dbSet.AnyAsync(s => s.Number == shelfNumber, cancellationToken);
}
