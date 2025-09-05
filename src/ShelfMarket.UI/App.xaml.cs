using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShelfMarket.Application;
using ShelfMarket.Infrastructure;
using ShelfMarket.Infrastructure.Persistence;

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
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                var env = context.HostingEnvironment.EnvironmentName;
                var connectionStringName = env == Environments.Development
                    ? "ShelfMarketDb_Development"
                    : "ShelfMarketDb";

                var connectionString = context.Configuration.GetConnectionString(connectionStringName);

                services.AddDbContext<ShelfMarketDbContext>(options =>
                    options.UseSqlServer(connectionString));

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
