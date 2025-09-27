using Microsoft.EntityFrameworkCore;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Domain.Entities;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure.Repositories;

public class ShelfTenantRepository : Repository<ShelfTenant>, IShelfTenantRepository
{
    public ShelfTenantRepository(ShelfMarketDbContext context) : base(context) { }

    public Task<ShelfTenant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult<ShelfTenant?>(null);

        var norm = email.Trim().ToLowerInvariant();
        return _context.Set<ShelfTenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Email.ToLower() == norm, cancellationToken);
    }
}