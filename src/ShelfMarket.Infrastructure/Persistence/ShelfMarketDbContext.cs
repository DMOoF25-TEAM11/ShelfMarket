using Microsoft.EntityFrameworkCore;

namespace ShelfMarket.Infrastructure.Persistence;

public class ShelfMarketDbContext : DbContext
{
    public ShelfMarketDbContext(DbContextOptions<ShelfMarketDbContext> options)
        : base(options)
    {
    }

}
