using Microsoft.EntityFrameworkCore;

namespace ShelfMarket.Infrastructure.Persistence;

public class ShelfMarketDbContext : DbContext
{
    public DbSet<Domain.Entities.ShelfType> ShelfTypes { get; set; }
    public DbSet<Domain.Entities.Shelf> Shelves { get; set; }

    public ShelfMarketDbContext(DbContextOptions<ShelfMarketDbContext> options)
        : base(options)
    {
    }

}
