using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Infrastructure.Persistence;
using ShelfMarket.UI.Views.Popups;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
/// <remarks>
/// This window initializes the main UI layout and performs a database connectivity check on load.
/// If the database is unreachable, an error dialog is shown to the user.
/// </remarks>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion

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

    #region Status properties
    private bool _isInternetAvailable;
    public bool IsInternetAvailable
    {
        get => _isInternetAvailable;
        private set
        {
            if (_isInternetAvailable != value)
            {
                _isInternetAvailable = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isDatabaseConnected;
    public bool IsDatabaseConnected
    {
        get => _isDatabaseConnected;
        private set
        {
            if (_isDatabaseConnected != value)
            {
                _isDatabaseConnected = value;
                OnPropertyChanged();
            }
        }
    }
    #endregion

    #region Timers/events
    private DispatcherTimer? _statusTimer;
    #endregion

    /// <summary>
    /// Constructs the main window, wires up lifecycle and connectivity events,
    /// and starts a periodic status timer for internet- og database-checks.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        //MainMenu.Content = new MainMenu(); // Placeholder
        //SideMenu.Content = new SideMenu(); // Placeholder

        Loaded += MainWindow_Loaded; // Kør initiale checks når vinduet er klar
        Closed += MainWindow_Closed; // Ryd op i events/timer ved luk

        // Lyt til ændringer i netværks-tilgængelighed (WiFi/Ethernet osv.)
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;

        // Initér status til "ukendt" indtil første aktive check er kørt
        IsInternetAvailable = false; // Vises som rød indtil vi har verificeret adgang
        IsDatabaseConnected = false; // Vises som rød indtil vi har verificeret forbindelse

        // Periodisk status-timer der opdaterer begge indikatorer
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15) // Justér hvis du vil have hurtigere/l langsommere opdatering
        };
        _statusTimer.Tick += StatusTimer_Tick; // På hvert tick opdateres begge statusser
        _statusTimer.Start(); // Start timeren
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
    /// <summary>
    /// Kaldes når vinduet er indlæst; udfører initialt internet- og database-check.
    /// Viser en fejlbesked hvis databasen ikke kan tilgås.
    /// </summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await UpdateInternetConnectivityAsync(); // Aktivt internetcheck ved opstart

        // Database-check ved opstart
        try
        {
            using var scope = App.HostInstance.Services.CreateScope(); // Opret DI-scope
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelfMarketDbContext>(); // Resolve DbContext

            var canConnect = await dbContext.Database.CanConnectAsync(); // Pinger databasen
            IsDatabaseConnected = canConnect; // Opdater indikator
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
            IsDatabaseConnected = false;
            MessageBox.Show(
                this,
                $"{_errorMsgContentDbCheck}:\n{ex.Message}",
                _errorMsgTitleDbCheck,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opdaterer database-indikatoren ved at spørge EF Core om databasen kan tilgås.
    /// Sætter <see cref="IsDatabaseConnected"/> til true/false afhængigt af resultatet.
    /// </summary>
    private async System.Threading.Tasks.Task UpdateDatabaseConnectivityAsync()
    {
        try
        {
            using var scope = App.HostInstance.Services.CreateScope(); // Opret DI-scope
            var dbContext = scope.ServiceProvider.GetRequiredService<ShelfMarketDbContext>(); // Resolve DbContext
            var canConnect = await dbContext.Database.CanConnectAsync(); // Hurtigt sundhedstjek
            IsDatabaseConnected = canConnect; // Opdater indikator
        }
        catch
        {
            IsDatabaseConnected = false; // Sæt til offline ved fejl/exception
        }
    }

    /// <summary>
    /// Aktivt internetcheck via HTTP request til en letvægts-URL der returnerer 204 (no content),
    /// som anbefalet til reachability tests. Opdaterer <see cref="IsInternetAvailable"/>.
    /// </summary>
    private async Task UpdateInternetConnectivityAsync()
    {
        try
        {
            using var httpClient = new HttpClient // Engangs-klient til lynhurtigt check
            {
                Timeout = TimeSpan.FromSeconds(3) // Kort timeout for responsiv UI
            };
            using var response = await httpClient.GetAsync("https://www.gstatic.com/generate_204"); // 204 hvis online
            IsInternetAvailable = ((int)response.StatusCode == 204) || response.IsSuccessStatusCode; // Grøn hvis online
        }
        catch
        {
            IsInternetAvailable = false; // Rød hvis request fejler/timeout
        }
    }

    /// <summary>
    /// Periodisk opdatering: kør begge checks (internet og database) uden at blokere UI.
    /// </summary>
    private async void StatusTimer_Tick(object? sender, EventArgs e)
    {
        await UpdateInternetConnectivityAsync(); // Opdater internetindikator
        await UpdateDatabaseConnectivityAsync(); // Opdater databaseindikator
    }

    /// <summary>
    /// Reagerer på ændringer i netværks-tilgængelighed (interfaces). Da dette ikke
    /// garanterer internetadgang, trigges et aktivt HTTP-baseret check.
    /// </summary>
    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        // Sørg for at køre på UI-tråden
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(async () => await UpdateInternetConnectivityAsync()); // Aktivt check
            return;
        }
        _ = UpdateInternetConnectivityAsync(); // Fire-and-forget check på UI-tråden
    }

    /// <summary>
    /// Rydder op i event-handlers og timer ved lukning for at undgå memory leaks.
    /// </summary>
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged; // Unsubscribe
        if (_statusTimer != null)
        {
            _statusTimer.Stop(); // Stop timer
            _statusTimer.Tick -= StatusTimer_Tick; // Unsubscribe fra tick
            _statusTimer = null; // Release ref
        }
        Loaded -= MainWindow_Loaded; // Unsubscribe
        Closed -= MainWindow_Closed; // Unsubscribe
    }

    private void SideMenuControl_Loaded(object sender, RoutedEventArgs e)
    {

    }
}