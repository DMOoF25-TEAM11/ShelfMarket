using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Domain.Enums;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;
using ShelfMarket.UI.Views;
using ShelfMarket.UI.Views.UserControls;

namespace ShelfMarket.UI.ViewModels;

public class SideMenuViewModel : ModelBase
{
    private readonly IPrivilegeService _privileges;

    public SideMenuViewModel()
    {
        _privileges = App.HostInstance.Services.GetRequiredService<IPrivilegeService>();
        _privileges.CurrentLevelChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(CurrentLevel));
            OnPropertyChanged(nameof(SideMenuItems)); // keep existing
            if (MenuItemCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
            RefreshMenuVisibilityAndSelection();
        };

        MenuItemCommand = new RelayCommand(OnCommand, CanCommand);
        SideMenuItems = new ObservableCollection<SideMenuItem>
        {
            new SideMenuItem("Log på", new LoginView(), PrivilegeLevel.Guest),
            new SideMenuItem("Log af", new ShelfView(), PrivilegeLevel.Guest, isLogoff: true),
            new SideMenuItem("Reoler", new ShelfView(), PrivilegeLevel.Guest),
            new SideMenuItem("Salg", new SalesView(), PrivilegeLevel.User),
            new SideMenuItem("Økonomi", new FinanceView(), PrivilegeLevel.Admin),
            new SideMenuItem("Arrangementer", new EventsView(), PrivilegeLevel.User),
            new SideMenuItem("Lejere", new ManagesShelfTenantView(), PrivilegeLevel.User),
            new SideMenuItem("Vedligeholdelse", new MaintenanceView(), PrivilegeLevel.User)
        };

        SelectedMenuItem ??= SideMenuItems[2];
    }

    public PrivilegeLevel CurrentLevel => _privileges.CurrentLevel;

    public class SideMenuItem
    {
        public SideMenuItem(string title, UserControl view, PrivilegeLevel privilege, bool isLogoff = false)
        {
            Title = title;
            View = view;
            Privilege = privilege;
            IsLogoff = isLogoff;
        }
        public string Title { get; init; }
        public UserControl View { get; init; }
        public PrivilegeLevel Privilege { get; init; }
        public bool IsLogoff { get; init; }
        public bool IsVisible { get; set; }
    }

    private SideMenuItem? _selectedMenuItem;
    public SideMenuItem? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (_selectedMenuItem == value) return;
            _selectedMenuItem = value;
            OnPropertyChanged(nameof(SelectedMenuItem));

            // Logoff: sign out, select "Log på", and navigate immediately
            if (value?.IsLogoff == true)
            {
                _privileges.SignOut();

                var login =
                    SideMenuItems.FirstOrDefault(i => i.Title == "Log på" && IsItemVisibleForCurrentLevel(i))
                    ?? SideMenuItems.FirstOrDefault(i => !i.IsLogoff && IsItemVisibleForCurrentLevel(i))
                    ?? SideMenuItems.FirstOrDefault();

                if (login != null)
                {
                    _selectedMenuItem = login;
                    OnPropertyChanged(nameof(SelectedMenuItem));
                    OnCommand(); // navigate to LoginView
                }

                if (MenuItemCommand is RelayCommand rcl) rcl.RaiseCanExecuteChanged();
                return; // avoid double navigation
            }

            OnCommand();
            if (MenuItemCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<SideMenuItem> SideMenuItems { get; } = new();

    public ICommand MenuItemCommand { get; }

    private bool CanCommand()
        => SelectedMenuItem is not null && _privileges.CanAccess(SelectedMenuItem.Privilege);

    private void OnCommand()
    {
        if (SelectedMenuItem is null || SelectedMenuItem.View == null) return;
        MainWindow? mainWindow = App.Current.MainWindow as MainWindow;
        if (mainWindow == null) return;
        if (App.Current.MainWindow is MainWindow mw && SelectedMenuItem?.View != null)
        {
            if (mw.MainContent != null)
                mw.MainContent.Content = SelectedMenuItem.View;
            else
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (mw.MainContent != null)
                        mw.MainContent.Content = SelectedMenuItem.View;
                }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // If the selected item was 'Log på', selection may become hidden after login.
        // CurrentLevelChanged will handle reselection; nothing else needed here.
    }

    private void RefreshMenuVisibilityAndSelection()
    {
        // Force the ItemsControl to re-evaluate item containers
        CollectionViewSource.GetDefaultView(SideMenuItems)?.Refresh();

        // Ensure selection is visible and accessible; otherwise select a sensible default
        if (_selectedMenuItem is null || !IsItemVisibleForCurrentLevel(_selectedMenuItem))
        {
            // Prefer "Log på" when guest; otherwise a non-logoff item (e.g., "Reoler")
            var next =
                (_privileges.CurrentLevel == PrivilegeLevel.Guest
                    ? SideMenuItems.FirstOrDefault(i => i.Title == "Log på" && IsItemVisibleForCurrentLevel(i))
                    : null)
                ?? SideMenuItems.FirstOrDefault(i => !i.IsLogoff && IsItemVisibleForCurrentLevel(i))
                ?? SideMenuItems.FirstOrDefault(IsItemVisibleForCurrentLevel)
                ?? SideMenuItems.FirstOrDefault();

            if (next != null && !ReferenceEquals(next, _selectedMenuItem))
            {
                _selectedMenuItem = next;
                OnPropertyChanged(nameof(SelectedMenuItem));

                // Navigate to the newly selected view
                OnCommand();
                if (MenuItemCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
            }
        }
    }

    private bool IsItemVisibleForCurrentLevel(SideMenuItem item)
    {
        if (!_privileges.CanAccess(item.Privilege)) return false;
        // Hide 'Log på' when logged in
        if (!item.IsLogoff && item.Title == "Log på" && _privileges.CurrentLevel != PrivilegeLevel.Guest) return false;
        // Hide 'Log af' when guest
        if (item.IsLogoff && _privileges.CurrentLevel == PrivilegeLevel.Guest) return false;
        return true;
    }
}
