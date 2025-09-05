using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShelfMarketInfrastructure(this IServiceCollection services)
    {

        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
#if RELEASE
        var connectionString = configuration.GetConnectionString("ShelfMarketDb");
#else
        var connectionString = configuration.GetConnectionString("ShelfMarketDb_Development");
#endif

        services.AddDbContext<ShelfMarketDbContext>(options =>
            options.UseSqlServer(connectionString));
        // Register infrastructure services here
        return services;
    }
}
