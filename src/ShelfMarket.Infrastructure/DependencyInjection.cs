using Microsoft.Extensions.DependencyInjection;

namespace ShelfMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddShelfMarketInfrastructure(this IServiceCollection services)
    {
        // Register infrastructure services here
        return services;
    }
}
