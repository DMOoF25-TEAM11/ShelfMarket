using Microsoft.EntityFrameworkCore;
using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Infrastructure.Persistence;

public class ShelfMarketDbContext : DbContext
{
    public DbSet<ShelfType> ShelfTypes { get; set; }
    public DbSet<Shelf> Shelves { get; set; }
    public DbSet<ShelfTenantContract> ShelfTenantContracts { get; set; }
    public DbSet<ShelfTenantContractLine> ShelfTenantContractLines { get; set; }
    
    public ShelfMarketDbContext(DbContextOptions<ShelfMarketDbContext> options)
        : base(options)
    {
    }

    //ShelfTenant
    public DbSet<ShelfTenant> ShelfTenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShelfType>()
            .ToTable("SHELFTYPE");
        modelBuilder.Entity<Shelf>()
            .ToTable("SHELF");
        // Map Orientation property to existing DB column name (typo in DB): OrientalHorizontal
        modelBuilder.Entity<Shelf>()
            .Property(s => s.OrientationHorizontal)
            .HasColumnName("OrientalHorizontal");
        modelBuilder.Entity<ShelfTenantContract>()
            .ToTable("SHELFTENANTCONTRACT");
        modelBuilder.Entity<ShelfTenantContractLine>()
            .ToTable("SHELFTENANTCONTRACTLINE");

        //ShelfTenant
        modelBuilder.Entity<ShelfTenant>()
            .ToTable("SHELFTENANT");
        modelBuilder.Entity<ShelfTenant>().Property(p => p.PhoneNumber).HasColumnName("PhoneNumber");
    }
}
