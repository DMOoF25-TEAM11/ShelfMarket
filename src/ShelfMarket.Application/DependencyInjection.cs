using Microsoft.Extensions.DependencyInjection;

namespace ShelfMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShelfMarketApplication(this IServiceCollection services)
    {
        // Register application services here
        return services;
    }
}
