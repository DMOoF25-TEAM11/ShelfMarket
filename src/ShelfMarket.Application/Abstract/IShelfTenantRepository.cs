using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Application.Abstract;

public interface IShelfTenantRepository : IRepository<ShelfTenant>
{
    Task<ShelfTenant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
