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
                services.AddTransient<ShelfTypeListViewModel>();
                services.AddTransient<ShelfTypeViewModel>();

                // ShelfTenantContract
                services.AddTransient<ManagesShelfTanentContractListViewModel>();
                services.AddTransient<ManagesShelfTanentContractViewModel>();

                // ShelfTenant
                services.AddSingleton<ITenantRepository, TenantRepository>();
                services.AddTransient<TenantsViewModel>();
                services.AddTransient<MainWindowViewModel>();
                services.AddSingleton(sp => new MainWindow
                {
                    DataContext = sp.GetRequiredService<MainWindowViewModel>()
                });

            })
            .Build();

        HostInstance.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }
}
