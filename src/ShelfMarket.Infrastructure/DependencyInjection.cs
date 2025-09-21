using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Infrastructure.Persistence;
using ShelfMarket.Infrastructure.Repositories;

namespace ShelfMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShelfMarketInfrastructure(this IServiceCollection services)
    {

        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("ShelfMarketDb");

        services.AddDbContext<ShelfMarketDbContext>(options =>
            options.UseSqlServer(connectionString));
        // Register infrastructure services here (Scoped to match DbContext lifetime)
        services.AddScoped<IShelfRepository, ShelfRepository>();
        services.AddScoped<IShelfTypeRepository, ShelfTypeRepository>();
        services.AddScoped<ISalesRepository, SalesRepository>();
        services.AddScoped<IShelfTenantContractRepository, ShelfTenantContractRepository>();
        return services;
    }
}
