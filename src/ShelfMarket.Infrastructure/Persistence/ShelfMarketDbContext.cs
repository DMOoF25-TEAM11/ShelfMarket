using Microsoft.EntityFrameworkCore;
using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Infrastructure.Persistence;

public class ShelfMarketDbContext : DbContext
{
    public DbSet<ShelfType> ShelfTypes { get; set; }
    public DbSet<Shelf> Shelves { get; set; }

    public ShelfMarketDbContext(DbContextOptions<ShelfMarketDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShelfType>()
            .ToTable("SHELFTYPE");
        modelBuilder.Entity<Shelf>()
            .ToTable("SHELF");
    }
}
