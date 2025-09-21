using Microsoft.EntityFrameworkCore;
using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Infrastructure.Persistence;

public class ShelfMarketDbContext : DbContext
{
    public DbSet<ShelfType> ShelfTypes { get; set; }
    public DbSet<Shelf> Shelves { get; set; }
    public DbSet<ShelfTenant> ShelfTenants { get; set; }
    public DbSet<ShelfTenantContract> ShelfTenantContracts { get; set; }
    public DbSet<ShelfTenantContractLine> ShelfTenantContractLines { get; set; }
    public DbSet<SalesLine> SalesLines { get; set; }
    public DbSet<Sales> Sales { get; set; }

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
        modelBuilder.Entity<ShelfTenant>()
            .ToTable("SHELFTENANT");
        modelBuilder.Entity<ShelfTenantContract>()
            .ToTable("SHELFTENANTCONTRACT");
        modelBuilder.Entity<ShelfTenantContractLine>()
            .ToTable("SHELFTENANTCONTRACTLINE");

        //ShelfTenant
        modelBuilder.Entity<ShelfTenant>()
            .ToTable("SHELFTENANT");
        modelBuilder.Entity<SalesLine>()
            .ToTable("SALESLINE");
        modelBuilder.Entity<Sales>()
            .ToTable("SALES");
    }
}
