using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Application.Abstract.Services.Barcodes;
using ShelfMarket.Application.Services;
using ShelfMarket.Application.Services.Barcodes;

namespace ShelfMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShelfMarketApplication(this IServiceCollection services)
    {
        services.AddScoped<IShelfLayoutService, ShelfLayoutService>();
        services.AddScoped<IEan13Generator, Ean13BarcodeGenerator>();
        services.AddSingleton<IPrivilegeService, PrivilegeService>();
        return services;
    }
}
