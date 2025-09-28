using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Application.Abstract.Services.Barcodes;
using ShelfMarket.Application.Services;
using ShelfMarket.Application.Services.Barcodes;

namespace ShelfMarket.Application;

/// <summary>
/// Provides extension methods for registering ShelfMarket application-layer services
/// with an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>
/// Service registrations and lifetimes:
///  - <see cref="IShelfLayoutService"/> => <see cref="ShelfLayoutService"/> (Scoped)
///  - <see cref="IEan13Generator"/> => <see cref="Ean13BarcodeGenerator"/> (Scoped)
///  - <see cref="IPrivilegeService"/> => <see cref="PrivilegeService"/> (Singleton)
/// 
/// The singleton lifetime for <see cref="IPrivilegeService"/> assumes a single-user
/// or session-wide privilege context. Adjust to scoped if per-request isolation is required.
/// </remarks>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the ShelfMarket application services into the provided <paramref name="services"/> collection.
    /// </summary>
    /// <param name="services">The DI service collection to augment.</param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance to enable fluent chaining.
    /// </returns>
    /// <remarks>
    /// This method only wires up application-layer abstractions. Persistence or infrastructure
    /// services should be registered in their respective infrastructure composition root.
    /// </remarks>
    public static IServiceCollection AddShelfMarketApplication(this IServiceCollection services)
    {
        services.AddScoped<IShelfLayoutService, ShelfLayoutService>();
        services.AddScoped<IEan13Generator, Ean13BarcodeGenerator>();
        services.AddSingleton<IPrivilegeService, PrivilegeService>();
        return services;
    }
}
