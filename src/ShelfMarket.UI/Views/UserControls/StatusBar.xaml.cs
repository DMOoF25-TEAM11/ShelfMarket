using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.UI.Views.UserControls
{
    /// <summary>
    /// Interaction logic for StatusBar.xaml
    /// </summary>
    public partial class StatusBar : UserControl
    {
        private readonly DispatcherTimer _pollTimer;
        private bool _isChecking;
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusBar"/> class and sets up periodic status checks.
        /// </summary>
        /// <remarks>The constructor initializes the status polling timer with a 10-second interval and
        /// attaches event handlers  to start and stop the timer when the control is loaded and unloaded, respectively.
        /// The first status check  is performed immediately after the control is loaded.</remarks>
        public StatusBar()
        {
            InitializeComponent();

            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _pollTimer.Tick += async (_, __) => await CheckStatusesAsync();

            Loaded += async (_, __) =>
            {
                await CheckStatusesAsync();
                _pollTimer.Start();
            };
            Unloaded += (_, __) =>
            {
                _pollTimer.Stop();
            };
        }
        /// <summary>
        /// Identifies the <see cref="IsInternetAvailable"/> dependency property.
        /// </summary>
        /// <remarks>This property indicates whether an internet connection is available.  It is a
        /// dependency property, which allows it to be used in data binding and styling scenarios.</remarks>
        public static readonly DependencyProperty IsInternetAvailableProperty = DependencyProperty.Register(
            nameof(IsInternetAvailable), typeof(bool), typeof(StatusBar), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether an active internet connection is available.
        /// </summary>
        public bool IsInternetAvailable
        {
            get => (bool)GetValue(IsInternetAvailableProperty);
            set => SetValue(IsInternetAvailableProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsDatabaseConnected"/> dependency property.
        /// </summary>
        /// <remarks>This property indicates whether the application is currently connected to the
        /// database. The default value is <see langword="false"/>.</remarks>
        public static readonly DependencyProperty IsDatabaseConnectedProperty = DependencyProperty.Register(
            nameof(IsDatabaseConnected), typeof(bool), typeof(StatusBar), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the database is currently connected.
        /// </summary>
        public bool IsDatabaseConnected
        {
            get => (bool)GetValue(IsDatabaseConnectedProperty);
            set => SetValue(IsDatabaseConnectedProperty, value);
        }

        /// <summary>
        /// Checks the current statuses of the internet connection and database connectivity asynchronously.
        /// </summary>
        /// <remarks>This method updates the <see cref="IsInternetAvailable"/> and <see
        /// cref="IsDatabaseConnected"/> properties to reflect the current availability of the internet and the database
        /// connection, respectively.  If the method is already running, it will return immediately without performing
        /// any checks.</remarks>
        /// <returns></returns>
        private async Task CheckStatusesAsync()
        {
            if (_isChecking) return;
            _isChecking = true;
            try
            {
                // Internet
                IsInternetAvailable = NetworkInterface.GetIsNetworkAvailable();

                // Database
                bool dbOk = false;
                try
                {
                    using var scope = App.HostInstance.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ShelfMarketDbContext>();
                    dbOk = await db.Database.CanConnectAsync();
                }
                catch
                {
                    dbOk = false;
                }
                IsDatabaseConnected = dbOk;
            }
            finally
            {
                _isChecking = false;
            }
        }
    }
}
