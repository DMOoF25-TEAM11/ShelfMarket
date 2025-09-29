using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShelfMarket.Application;
using ShelfMarket.Infrastructure;
using ShelfMarket.UI.ViewModels;
using ShelfMarket.UI.ViewModels.Reports;

namespace ShelfMarket.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public static IHost HostInstance { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        HostInstance = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register application and infrastructure layers
                services
                    .AddShelfMarketApplication()
                    .AddShelfMarketInfrastructure();

                // Register MainWindow
                services.AddTransient<MainWindow>();

                // Register Shelf
                services.AddSingleton<ShelfViewModel>();

                // ShelfType
                //services.AddTransient<ManagesShelfTypeListViewModel>();
                //services.AddTransient<ManagesShelfTypeViewModel>();

                // ShelfTenantContract
                // Scoped: each popup gets its own scoped VM/DbContext to avoid
                // cross-thread DbContext reuse issues.
                services.AddScoped<ManagesShelfTenantContractViewModel>();
                //services.AddTransient<ManagesShelfTanentContractListViewModel>();
                //services.AddTransient<ManagesShelfTenantContractListViewModel>();

                // ManagesShelfTenant
                services.AddTransient<ManagesShelfTenantViewModel>();

                // ShelfTenant
                //services.AddSingleton<IShelfTenantRepository, ShelfTenantRepository>();
                //services.AddTransient<TenantsViewModel>();
                //services.AddTransient<MainWindowViewModel>();

                services.AddTransient<ReportDailyCashViewModel>();
            })
            .Build();

        HostInstance.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }
}
