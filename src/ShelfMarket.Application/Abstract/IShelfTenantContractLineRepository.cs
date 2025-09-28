using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

/// <summary>
/// Repository abstraction for managing <see cref="ShelfTenantContractLine"/> entities.
/// </summary>
/// <remarks>
/// Extends the generic <see cref="IRepository{TEntity}"/> without adding new members (marker interface).
/// Provides a clearer intent boundary for dependency injection and future contract‑specific queries.
/// </remarks>
public interface IShelfTenantContractLineRepository : IRepository<ShelfTenantContractLine>
{
}
