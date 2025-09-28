using Microsoft.EntityFrameworkCore;
using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core <see cref="DbContext"/> for the ShelfMarket domain.
/// </summary>
/// <remarks>
/// Contains <see cref="DbSet{TEntity}"/> properties for all aggregate roots / entities
/// persisted by the application. Table names are explicitly mapped to uppercase names
/// to match an expected legacy / existing database naming convention.
/// 
/// Notes:
/// - Some advanced configurations (identity columns, precision, unique indices) are
///   present but currently commented out; they can be re‑enabled as the domain model
///   stabilizes.
/// - The context assumes that entity classes expose nullable <see cref="Guid"/> keys
///   which are assigned externally or by the database depending on configuration.
/// </remarks>
public class ShelfMarketDbContext : DbContext
{
    /// <summary>
    /// Shelf type definitions (e.g., size categories, descriptors).
    /// </summary>
    public DbSet<ShelfType> ShelfTypes { get; set; }

    /// <summary>
    /// Physical or logical shelves available in the marketplace.
    /// </summary>
    public DbSet<Shelf> Shelves { get; set; }

    /// <summary>
    /// Tenants (customers) who can rent / occupy shelves.
    /// </summary>
    public DbSet<ShelfTenant> ShelfTenants { get; set; }

    /// <summary>
    /// Contract headers between a tenant and the shelf provider.
    /// </summary>
    public DbSet<ShelfTenantContract> ShelfTenantContracts { get; set; }

    /// <summary>
    /// Individual contract line allocations (specific shelves within a contract).
    /// </summary>
    public DbSet<ShelfTenantContractLine> ShelfTenantContractLines { get; set; }

    /// <summary>
    /// Sales line items (per shelf / EAN sale details).
    /// </summary>
    public DbSet<SalesLine> SalesLines { get; set; }

    /// <summary>
    /// Sales headers (grouping sales lines, with payment metadata).
    /// </summary>
    public DbSet<Sales> Sales { get; set; }

    /// <summary>
    /// Tiered pricing rules used to determine per-shelf pricing based on quantity.
    /// </summary>
    public DbSet<ShelfPricingRule> ShelfPricingRules { get; set; } // add DbSet

    /// <summary>
    /// Creates a new context with the specified options configuration.
    /// </summary>
    /// <param name="options">Configured <see cref="DbContextOptions{TContext}"/>.</param>
    public ShelfMarketDbContext(DbContextOptions<ShelfMarketDbContext> options) : base(options) { }

    /// <summary>
    /// Applies model configuration such as table mappings and (optional) constraints.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure EF Core metadata.</param>
    /// <remarks>
    /// Currently maps each entity to an uppercase table name. Additional configurations
    /// (identity columns, precision, unique indexes) are left commented for future activation.
    /// </remarks>
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

    }
}
