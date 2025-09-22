using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.UI.Views.UserControls
{
    public partial class StatusBar : UserControl
    {
        private readonly DispatcherTimer _pollTimer;
        private bool _isChecking;
        private readonly IPrivilegeService _privileges;

        public StatusBar()
        {
            InitializeComponent();

            _privileges = App.HostInstance.Services.GetRequiredService<IPrivilegeService>();
            _privileges.CurrentLevelChanged += OnCurrentLevelChanged;
            CurrentUserRole = _privileges.CurrentLevel.ToString();

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
                _privileges.CurrentLevelChanged -= OnCurrentLevelChanged;
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
                IsInternetAvailable = NetworkInterface.GetIsNetworkAvailable();

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

                // Keep role in sync with the service
                CurrentUserRole = _privileges.CurrentLevel.ToString();
            }
            finally
            {
                _isChecking = false;
            }
        }

        private void OnCurrentLevelChanged(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentUserRole = _privileges.CurrentLevel.ToString();
            });
        }
    }
}
