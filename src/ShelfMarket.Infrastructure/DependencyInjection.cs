using Microsoft.Data.SqlClient; // added
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract;
using ShelfMarket.Infrastructure.Persistence;
using ShelfMarket.Infrastructure.Repositories;

namespace ShelfMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShelfMarketInfrastructure(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
#if DEBUG
        var connectionString = configuration.GetConnectionString("ShelfMarketDb_Dev");
#else
        var connectionString = configuration.GetConnectionString("ShelfMarketDb");
#endif
        // Force-enable MARS to allow multiple active readers on the same connection
        var csb = new SqlConnectionStringBuilder(connectionString)
        {
            MultipleActiveResultSets = true
        };

        services.AddDbContext<ShelfMarketDbContext>(options =>
            options.UseSqlServer(csb.ConnectionString));

        // Repositories
        services.AddScoped<IShelfRepository, ShelfRepository>();
        services.AddScoped<IShelfTypeRepository, ShelfTypeRepository>();
        services.AddScoped<ISalesRepository, SalesRepository>();
        services.AddScoped<IShelfTenantRepository, ShelfTenantRepository>();
        services.AddScoped<IShelfTenantContractRepository, ShelfTenantContractRepository>();
        services.AddScoped<IShelfTenantContractLineRepository, ShelfTenantContractLineRepository>();
        services.AddSingleton<IShelfPricingRuleRepository, ShelfPricingRuleRepository>();

        return services;
    }
}
