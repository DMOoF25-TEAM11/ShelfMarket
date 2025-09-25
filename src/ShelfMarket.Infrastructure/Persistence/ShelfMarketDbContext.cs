using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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

    public ShelfMarketDbContext(DbContextOptions<ShelfMarketDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ShelfType>().ToTable("SHELFTYPE");
        modelBuilder.Entity<Shelf>().ToTable("SHELF");
        modelBuilder.Entity<ShelfTenant>().ToTable("SHELFTENANT");
        modelBuilder.Entity<ShelfTenantContract>().ToTable("SHELFTENANTCONTRACT");
        modelBuilder.Entity<ShelfTenantContractLine>().ToTable("SHELFTENANTCONTRACTLINE");
        modelBuilder.Entity<SalesLine>().ToTable("SALESLINE");
        modelBuilder.Entity<Sales>().ToTable("SALES");

        modelBuilder.Entity<ShelfTenantContract>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasDefaultValueSql("NEWID()");
            b.Property(e => e.ContractNumber)
                .ValueGeneratedOnAdd();
            // Ensure EF never tries to update the identity column
            b.Property(e => e.ContractNumber).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            b.Property(e => e.ContractNumber).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        });

        // Ensure EF treats LineNumber as IDENTITY (database generated) and never sends explicit values
        //modelBuilder.Entity<ShelfTenantContractLine>(b =>
        //{
        //    b.HasKey(e => e.Id);
        //    b.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        //    b.Property(e => e.LineNumber).ValueGeneratedOnAdd();
        //    b.Property(e => e.LineNumber).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        //    b.Property(e => e.LineNumber).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        //    // Optional: silence precision warnings
        //    b.Property(e => e.PricePerMonth).HasPrecision(18, 2);
        //    b.Property(e => e.PricePerMonthSpecial).HasPrecision(18, 2);
        //});
    }
}
