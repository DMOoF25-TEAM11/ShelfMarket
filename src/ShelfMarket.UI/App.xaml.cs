using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShelfMarket.Application;
using ShelfMarket.Infrastructure;

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
            })
            .Build();

        HostInstance.Services.GetRequiredService<MainWindow>().Show();
        base.OnStartup(e);
    }
}
