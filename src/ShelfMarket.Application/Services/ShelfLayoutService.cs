using Microsoft.Extensions.Logging;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Services;

/// <summary>
/// Coordinates shelf layout reads/updates with domain and repositories.
/// Enforces basic rules (no overlap/locked area) via repository helpers.
/// </summary>
public sealed class ShelfLayoutService : IShelfLayoutService
{
    private readonly IShelfRepository _shelfRepository; // to check for overlaps
    private readonly IShelfTypeRepository _shelfTypeRepository; // to pick a type when creating a shelf
    private readonly ILogger<ShelfLayoutService> _logger; // for logging

    /// <summary>
    /// Initializes a new instance of the <see cref="ShelfLayoutService"/> class.
    /// </summary>
    /// <param name="shelfRepository">The repository used to manage shelf data.</param>
    /// <param name="shelfTypeRepository">The repository used to manage shelf type data.</param>
    /// <param name="logger">The logger instance used for logging operations and diagnostics.</param>
    public ShelfLayoutService(IShelfRepository shelfRepository, IShelfTypeRepository shelfTypeRepository, ILogger<ShelfLayoutService> logger)
    {
        _shelfRepository = shelfRepository;
        _shelfTypeRepository = shelfTypeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Asynchronously retrieves all shelves from the repository.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="IEnumerable{T}"/> of
    /// <see cref="Shelf"/> objects.</returns>
    public Task<IEnumerable<Shelf>> GetAllAsync(CancellationToken cancellationToken = default)
        => _shelfRepository.GetAllAsync(cancellationToken);

    public async Task<bool> TryUpdatePositionAsync(int shelfNumber, int locationX, int locationY, bool orientationHorizontal, CancellationToken cancellationToken = default)
    {
        var all = await _shelfRepository.GetAllAsync(cancellationToken);
        var shelf = all.FirstOrDefault(s => s.Number == shelfNumber);
        if (shelf == null)
        {
            _logger.LogWarning("Shelf {Number} not found when updating position", shelfNumber);
            return false;
        }

        // Allow orientation changes in-place. Only block when target cell is occupied by another shelf.
        bool occupiedByOther = all.Any(s => s.LocationX == locationX && s.LocationY == locationY && s.Number != shelfNumber);
        if (occupiedByOther)
        {
            _logger.LogInformation("Location ({X},{Y}) is occupied by another shelf when updating {Number}", locationX, locationY, shelfNumber);
            return false;
        }

        shelf.LocationX = locationX;
        shelf.LocationY = locationY;
        shelf.OrientationHorizontal = orientationHorizontal;
        await _shelfRepository.UpdateAsync(shelf, cancellationToken);
        return true;
    }

    public async Task<bool> TryCreateShelfAsync(int number, bool orientationHorizontal, int locationX = 22, int locationY = 0, CancellationToken cancellationToken = default)
    {
        var all = await _shelfRepository.GetAllAsync(cancellationToken);
        if (all.Any(s => s.Number == number))
        {
            _logger.LogInformation("Shelf number {Number} already exists", number);
            return false;
        }
        if (!await _shelfRepository.IsLocationFree(locationX, locationY, cancellationToken))
        {
            _logger.LogInformation("Location ({X},{Y}) is occupied when creating shelf {Number}", locationX, locationY, number);
            return false;
        }

        // Pick a shelf type. If none exists, create a simple 'Default'.
        var types = await _shelfTypeRepository.GetAllAsync(cancellationToken);
        var first = types.FirstOrDefault();
        var typeId = first?.Id ?? Guid.Empty;
        if (typeId == Guid.Empty)
        {
            var defaultType = new ShelfType { Id = Guid.NewGuid(), Name = "Default", Description = "Auto-created" };
            await _shelfTypeRepository.AddAsync(defaultType, cancellationToken);
            typeId = defaultType.Id!.Value;
        }

        var shelf = new Shelf(number, typeId, locationX, locationY, orientationHorizontal);
        await _shelfRepository.AddAsync(shelf, cancellationToken);
        return true;
    }
}


