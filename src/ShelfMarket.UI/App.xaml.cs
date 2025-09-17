using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShelfMarket.Application;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.Infrastructure;
using ShelfMarket.Infrastructure.Repositories;
using ShelfMarket.UI.ViewModels;
using System.Windows;

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

                // ShelfType
                services.AddTransient<ManagesShelfTypeListViewModel>();
                services.AddTransient<ManagesShelfTypeViewModel>();

                // ShelfTenantContract
                services.AddTransient<ManagesShelfTanentContractListViewModel>();
                services.AddTransient<ManagesShelfTanentContractViewModel>();

                // ShelfTenant
                services.AddSingleton<ITenantRepository, TenantRepository>();
                services.AddTransient<TenantsViewModel>();
                services.AddTransient<MainWindowViewModel>();
                

            })
            .Build();

        HostInstance.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }
}
