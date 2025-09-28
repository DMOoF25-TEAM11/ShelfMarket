using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

/// <summary>
/// Repository abstraction for managing <see cref="ShelfTenant"/> entities,
/// extending the generic <see cref="IRepository{TEntity}"/> with tenant-specific lookups.
/// </summary>
/// <remarks>
/// Primary extension is an indexed/email-based lookup via <see cref="GetByEmailAsync(string, CancellationToken)"/>.
/// Implementations should:
///  - Treat email matching case-insensitively unless domain rules require otherwise.
///  - Ensure any unique email constraints are enforced at persistence level to keep lookup deterministic.
/// </remarks>
public interface IShelfTenantRepository : IRepository<ShelfTenant>
{
    /// <summary>
    /// Retrieves a tenant by email address.
    /// </summary>
    /// <param name="email">Email address to search for (expected to be a well‑formed address).</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// The matching <see cref="ShelfTenant"/> if found; otherwise <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Implementations should normalize (e.g. trim, to-lower) the input consistently prior to lookup.
    /// If multiple records somehow match (data inconsistency), the first deterministic record should be returned
    /// or an exception may be thrown depending on policy (document behavior).
    /// </remarks>
    Task<ShelfTenant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
