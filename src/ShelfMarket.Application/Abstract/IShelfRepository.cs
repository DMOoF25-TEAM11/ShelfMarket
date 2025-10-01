using ShelfMarket.Application.DTOs;
using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

/// <summary>
/// Repository abstraction for querying and managing <see cref="Shelf"/> entities
/// plus domain-specific availability queries used when allocating or reserving shelves.
/// </summary>
/// <remarks>
/// Extends <see cref="IRepository{TEntity}"/> with:
///  - Spatial occupancy checks (<see cref="IsLocationFreeAsync"/>)
///  - Availability discovery for a general period (<see cref="GetAvailableShelves"/>)
///  - Availability filtered for a specific tenant (<see cref="GetAvailableShelvesForTenantAsync"/>)
/// Implementations should:
///  - Treat non-existing entities gracefully (e.g. occupancy check returns false only when actually occupied).
///  - Enforce any business constraints regarding blocked or reserved coordinates inside
///    <see cref="IsLocationFreeAsync"/> so that callers can rely on a single authoritative check.
///  - Use consistent timezone semantics for date arguments (prefer UTC or clearly documented local time).
/// </remarks>
public interface IShelfRepository : IRepository<Shelf>
{
    /// <summary>
    /// Determines whether the specified grid position (and orientation context) is currently free for use.
    /// </summary>
    /// <param name="locationX">The X (column) coordinate.</param>
    /// <param name="locationY">The Y (row) coordinate.</param>
    /// <param name="orientationHorizontal">
    /// Orientation the caller intends to place. Implementations may consider orientation when
    /// evaluating multi-cell occupancy or orientation-specific constraints.
    /// </param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// <c>true</c> if no shelf (or reserved/blocked rule) occupies the coordinate for the specified orientation;
    /// otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method should incorporate domain rules about:
    ///  - Existing shelf presence.
    ///  - Blocked / restricted cells.
    ///  - Orientation-based conflicts (if a horizontal shelf spans multiple cells, etc.).
    /// Callers use this prior to attempting moves or creation to provide immediate user feedback.
    /// </remarks>
    Task<bool> IsLocationFreeAsync(
        int locationX,
        int locationY,
        bool orientationHorizontal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves shelves that are available (not booked / allocated) for the entire inclusive interval.
    /// </summary>
    /// <param name="startDate">Start of the requested availability period (inclusive).</param>
    /// <param name="endDate">End of the requested availability period (exclusive or inclusive as defined by implementation; should be documented consistently).</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// An enumeration of lightweight shelf availability DTOs representing shelves free during the interval.
    /// </returns>
    /// <remarks>
    /// Implementations should define:
    ///  - Whether <paramref name="endDate"/> is treated as inclusive or exclusive (common: exclusive).
    ///  - Handling when <paramref name="startDate"/> &gt;= <paramref name="endDate"/> (commonly empty result).
    /// </remarks>
    Task<IEnumerable<AvailableShelfDto>> GetAvailableShelves(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves shelves available to (or eligible for) a given tenant for a time window—taking into account
    /// existing reservations made by that tenant or others depending on business rules.
    /// </summary>
    /// <param name="tenantId">The tenant identifier requesting availability.</param>
    /// <param name="startDate">Start of requested availability period.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// Shelves available for the tenant during the interval (possibly including shelves already
    /// reserved by the tenant if extensions/modifications are permitted—implementation specific).
    /// </returns>
    /// <remarks>
    /// Common behavior: show shelves either entirely free or already associated with the same tenant
    /// in a compatible booking state. Implementers should document any deviation.
    /// </remarks>
    Task<IEnumerable<AvailableShelfDto>> GetAvailableShelvesForTenantAsync(
        Guid tenantId,
        DateTime startDate,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsShelfNumberAsync(int shelfNumber, CancellationToken cancellationToken = default);
}
