using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract.Services;

/// <summary>
/// Application service concerned with shelf layout (grid positions and orientation).
/// </summary>
/// <remarks>
/// This service abstracts persistence and domain rules around physical / logical shelf placement.
/// Implementations are expected to enforce invariants such as:
///  - Shelf numbers are unique.
///  - A grid cell can be occupied by at most one shelf (per orientation rule set).
///  - Position/orientation updates should fail (return <c>false</c>) rather than throw for typical
///    business rule violations (e.g. target cell already occupied), reserving exceptions for
///    unexpected/internal errors.
/// All methods are asynchronous to support I/O‑bound backing stores (database, remote API, etc.).
/// </remarks>
public interface IShelfLayoutService
{
    /// <summary>
    /// Retrieves all shelves currently registered in the layout.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task producing an enumeration of <see cref="Shelf"/> entities. Implementations may return
    /// the shelves in an arbitrary order unless otherwise documented.
    /// </returns>
    Task<IEnumerable<Shelf>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to update the grid position and orientation of an existing shelf.
    /// </summary>
    /// <param name="shelfNumber">The unique shelf number identifying the shelf to move.</param>
    /// <param name="locationX">Target X (column) coordinate in the layout grid.</param>
    /// <param name="locationY">Target Y (row) coordinate in the layout grid.</param>
    /// <param name="orientationHorizontal">
    /// <c>true</c> to set the shelf orientation to horizontal; <c>false</c> to set vertical (or alternative) orientation.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task producing <c>true</c> if the update succeeded; <c>false</c> if the shelf does not exist
    /// or the target position/orientation violates a layout constraint (e.g. position occupied).
    /// </returns>
    Task<bool> TryUpdatePositionAsync(
        int shelfNumber,
        int locationX,
        int locationY,
        bool orientationHorizontal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to create and place a new shelf at a given grid position.
    /// </summary>
    /// <param name="number">Desired unique shelf number for the new shelf.</param>
    /// <param name="orientationHorizontal">
    /// <c>true</c> to create the shelf with horizontal orientation; <c>false</c> for vertical (or alternative) orientation.
    /// </param>
    /// <param name="locationX">Initial X (column) coordinate (defaults to 22).</param>
    /// <param name="locationY">Initial Y (row) coordinate (defaults to 0).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task producing <c>true</c> if the shelf was created; <c>false</c> if the shelf number already
    /// exists or the target cell is occupied.
    /// </returns>
    /// <remarks>
    /// Implementations should not throw for common business rule failures; they should return <c>false</c> instead.
    /// </remarks>
    Task<bool> TryCreateShelfAsync(
        int number,
        bool orientationHorizontal,
        int locationX = 22,
        int locationY = 0,
        CancellationToken cancellationToken = default);
}


