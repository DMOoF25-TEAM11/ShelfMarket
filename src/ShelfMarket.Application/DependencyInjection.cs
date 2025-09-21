using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstracts.Services;
using ShelfMarket.Application.Services;

namespace ShelfMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShelfMarketApplication(this IServiceCollection services)
    {
        // Register application services here
        services.AddScoped<IShelfLayoutService, ShelfLayoutService>();
        return services;
    }
}
