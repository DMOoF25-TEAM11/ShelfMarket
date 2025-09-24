namespace ShelfMarket.Application.Abstract;

/// <summary>
/// Defines a generic asynchronous CRUD repository abstraction.
/// Implementations may be purely in-memory or provide persistence.
/// </summary>
/// <typeparam name="TEntity">The entity type managed by the repository.</typeparam>
public interface IRepository<TEntity>
    where TEntity : class
{
    #region Create operations
    /// <summary>
    /// Adds a single entity to the repository. Generates a new identifier if necessary.
    /// </summary>
    /// <param name="entity">Entity instance to add.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the repository sequentially.
    /// </summary>
    /// <param name="entities">Entities to add.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    #endregion

    #region Read operations

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>The matching entity, or <c>null</c> if not found.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities as a snapshot enumeration.
    /// </summary>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>All current entities.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Update operations

    /// <summary>
    /// Replaces the stored entity matching the identifier contained in <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">Entity containing updated state.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    #endregion

    #region Delete operations

    /// <summary>
    /// Removes an entity by identifier. No-op if the entity does not exist.
    /// </summary>
    /// <param name="id">Identifier of the entity to remove.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    #endregion
}
