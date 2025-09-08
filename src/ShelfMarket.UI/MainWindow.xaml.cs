using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
/// <remarks>
/// This window initializes the main UI layout and performs a database connectivity check on load.
/// If the database is unreachable, an error dialog is shown to the user.
/// </remarks>
public partial class MainWindow : Window
{
    #region Private constants
    /// <summary>
    /// The dialog title used when the application cannot connect to the database.
    /// </summary>
    private const string _errorMsgTitleDbConnection = "Fejl: Database Forbindelse.";

    /// <summary>
    /// The dialog message content shown when a database connection could not be established.
    /// </summary>
    private const string _errorMsgContentDbConnection = "Applikationen kunne ikke forbinde til databasen. Venligst tjek forbindelsesstrengen og database tilgængelighed.";

    /// <summary>
    /// The dialog title used when an unexpected error occurs during the database connectivity check.
    /// </summary>
    private const string _errorMsgTitleDbCheck = "Fejl: Database Forbindelse Tjek.";

    /// <summary>
    /// The dialog message prefix used when an unexpected exception is thrown during the connection check.
    /// </summary>
    private const string _errorMsgContentDbCheck = "Database forbindelses tjek fejlede.";
    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class, sets up the main and side menus,
    /// and subscribes to the <see cref="FrameworkElement.Loaded"/> event to verify database connectivity.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        //MainMenu.Content = new MainMenu();
        //SideMenu.Content = new SideMenu();

        Loaded += MainWindow_Loaded;
    }

    /// <summary>
    /// Handles the window <see cref="FrameworkElement.Loaded"/> event and checks if the application
    /// can connect to the configured database using a scoped <see cref="ShelfMarketDbContext"/>.
    /// </summary>
    /// <param name="sender">The source of the event, typically the current <see cref="MainWindow"/>.</param>
    /// <param name="e">Event data containing information about the load event.</param>
    /// <remarks>
    /// If the connection cannot be established or an exception is thrown, a message box is displayed
    /// to inform the user about the issue. This handler is asynchronous and intentionally returns void
    /// because it is an event handler.
    /// </remarks>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Check database connectivity when the window loads
        try
        {
            using var scope = App.HostInstance.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelfMarketDbContext>();

            var canConnect = await dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                MessageBox.Show(
                    this,
                    _errorMsgContentDbConnection,
                    _errorMsgTitleDbConnection,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"{_errorMsgContentDbCheck}:\n{ex.Message}",
                _errorMsgTitleDbCheck,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}