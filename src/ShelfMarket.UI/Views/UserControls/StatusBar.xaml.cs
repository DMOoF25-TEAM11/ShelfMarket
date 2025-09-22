using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
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

        public static readonly DependencyProperty IsInternetAvailableProperty = DependencyProperty.Register(
            nameof(IsInternetAvailable), typeof(bool), typeof(StatusBar), new PropertyMetadata(false));

        public bool IsInternetAvailable
        {
            get => (bool)GetValue(IsInternetAvailableProperty);
            set => SetValue(IsInternetAvailableProperty, value);
        }

        public static readonly DependencyProperty IsDatabaseConnectedProperty = DependencyProperty.Register(
            nameof(IsDatabaseConnected), typeof(bool), typeof(StatusBar), new PropertyMetadata(false));

        public bool IsDatabaseConnected
        {
            get => (bool)GetValue(IsDatabaseConnectedProperty);
            set => SetValue(IsDatabaseConnectedProperty, value);
        }

        // Active user role text
        public static readonly DependencyProperty CurrentUserRoleProperty = DependencyProperty.Register(
            nameof(CurrentUserRole), typeof(string), typeof(StatusBar), new PropertyMetadata("Guest"));

        public string CurrentUserRole
        {
            get => (string)GetValue(CurrentUserRoleProperty);
            set => SetValue(CurrentUserRoleProperty, value);
        }

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

                // Active user role (reads from privilege service if available)
                CurrentUserRole = GetCurrentRoleString();
            }
            finally
            {
                _isChecking = false;
            }
        }

        private static string GetCurrentRoleString()
        {
            try
            {
                using var scope = App.HostInstance.Services.CreateScope();
                var svc = scope.ServiceProvider.GetService<IPrivilegeService>();
                return svc?.CurrentLevel.ToString() ?? "Guest";
            }
            catch
            {
                return "Guest";
            }
        }
    }
}
