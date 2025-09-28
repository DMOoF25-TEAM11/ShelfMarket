using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Domain.Enums;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;
using ShelfMarket.UI.Views;
using ShelfMarket.UI.Views.UserControls;

namespace ShelfMarket.UI.ViewModels;

/// <summary>
/// ViewModel controlling the side navigation menu behavior.
/// Responsibilities:
///  - Maintains menu items and current selection
///  - Enforces privilege based visibility and access
///  - Handles login / logoff transitions
///  - Navigates the main window content area to the selected view
/// Optimizations:
///  - Caches references to special items (login/logoff/default)
///  - Avoids redundant navigation when the target view is already displayed
///  - Uses direct loops over LINQ for lower overhead (predictable small collection)
///  - Reuses instantiated <see cref="UserControl"/> instances where appropriate
/// </summary>
public sealed class SideMenuViewModel : ModelBase
{
    /// <summary>
    /// Privilege service used to evaluate and react to privilege changes.
    /// </summary>
    private readonly IPrivilegeService _privileges;

    /// <summary>
    /// Command executing navigation of the selected menu item.
    /// </summary>
    private readonly RelayCommand _menuItemCommand;

    /// <summary>
    /// Cached login menu item (visible only for Guests).
    /// </summary>
    private readonly SideMenuItem _loginItem;

    /// <summary>
    /// Cached logoff menu item (hidden for Guests).
    /// </summary>
    private readonly SideMenuItem _logoffItem;

    /// <summary>
    /// Default (home) menu item selected at startup.
    /// </summary>
    private readonly SideMenuItem _defaultItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="SideMenuViewModel"/> class,
    /// wires privilege change notifications, builds the menu, and selects the default item.
    /// </summary>
    public SideMenuViewModel()
    {
        _privileges = App.HostInstance.Services.GetRequiredService<IPrivilegeService>();
        _privileges.CurrentLevelChanged += OnPrivilegeLevelChanged;

        // Shared ShelfViewModel instance reused across shelf-related views
        var shelfViewModel = App.HostInstance.Services.GetRequiredService<ShelfViewModel>();

        // Pre-create and cache views (reuse instances instead of recreating per navigation)
        _loginItem = new SideMenuItem("Log på", new LoginView(), PrivilegeLevel.Guest);

        var logoffShelfView = new ShelfView { DataContext = shelfViewModel };
        _logoffItem = new SideMenuItem("Log af", logoffShelfView, PrivilegeLevel.Guest, isLogoff: true);

        var defaultShelfView = new ShelfView { DataContext = shelfViewModel };
        _defaultItem = new SideMenuItem("Reoler", defaultShelfView, PrivilegeLevel.Guest);

        // Populate (order preserved – bindings may rely on it)
        SideMenuItems =
        [
            _loginItem,
            _logoffItem,
            _defaultItem,
            new("Salg",            new SalesView(),               PrivilegeLevel.User),
            new("Økonomi",         new FinanceView(),             PrivilegeLevel.Admin),
            new("Arrangementer",   new EventsView(),              PrivilegeLevel.User),
            new("Lejere",          new ManagesShelfTenantView(),  PrivilegeLevel.User),
            new("Vedligeholdelse", new MaintenanceView(),         PrivilegeLevel.User)
        ];

        _menuItemCommand = new RelayCommand(ExecuteNavigation, CanNavigate);
        MenuItemCommand = _menuItemCommand;

        _selectedMenuItem = _defaultItem;
        OnPropertyChanged(nameof(SelectedMenuItem));

        // Immediate navigation (works if MainWindow already created)
        ExecuteNavigation();

        // Deferred navigation (covers case where MainWindow not ready at construction time)
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            new Action(() =>
            {
                if (ReferenceEquals(_selectedMenuItem, _defaultItem))
                    ExecuteNavigation();
            }),
            DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Gets the current privilege level (proxy for binding).
    /// </summary>
    public PrivilegeLevel CurrentLevel => _privileges.CurrentLevel;

    /// <summary>
    /// Represents a navigable side menu entry (title + view + privilege requirement).
    /// </summary>
    public sealed class SideMenuItem
    {
        /// <summary>
        /// Creates a new side menu item.
        /// </summary>
        /// <param name="title">Display title shown in the menu.</param>
        /// <param name="view">Associated view to navigate when selected.</param>
        /// <param name="privilege">Minimum privilege level required to access.</param>
        /// <param name="isLogoff">True if this item triggers logout behavior instead of navigation.</param>
        public SideMenuItem(string title, UserControl view, PrivilegeLevel privilege, bool isLogoff = false)
        {
            Title = title;
            View = view;
            Privilege = privilege;
            IsLogoff = isLogoff;
        }

        /// <summary>
        /// Gets the display title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the associated view instance for navigation.
        /// </summary>
        public UserControl View { get; }

        /// <summary>
        /// Gets the required privilege to access this item.
        /// </summary>
        public PrivilegeLevel Privilege { get; }

        /// <summary>
        /// Gets a value indicating whether this item triggers logout logic.
        /// </summary>
        public bool IsLogoff { get; }

        /// <summary>
        /// Optional cached visibility flag (currently unused; available for future filtering optimizations).
        /// </summary>
        public bool IsVisible { get; set; }
    }

    /// <summary>
    /// Backing field for <see cref="SelectedMenuItem"/>.
    /// </summary>
    private SideMenuItem? _selectedMenuItem;

    /// <summary>
    /// Gets or sets the currently selected menu item.
    /// Setting this property executes navigation or logout logic as appropriate.
    /// </summary>
    public SideMenuItem? SelectedMenuItem
    {
        get => _selectedMenuItem;
        set
        {
            if (ReferenceEquals(_selectedMenuItem, value) || value is null)
                return;

            _selectedMenuItem = value;
            OnPropertyChanged(nameof(SelectedMenuItem));

            if (value.IsLogoff)
            {
                // Sign out and redirect to a valid item without re-processing the logoff item
                _privileges.SignOut();

                var next = GetPostLogoffSelection();
                if (!ReferenceEquals(next, _selectedMenuItem))
                {
                    _selectedMenuItem = next;
                    OnPropertyChanged(nameof(SelectedMenuItem));
                    ExecuteNavigation();
                }

                _menuItemCommand.RaiseCanExecuteChanged();
                return;
            }

            ExecuteNavigation();
            _menuItemCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets the collection of all menu items (visibility enforced at binding via privilege converters or checks).
    /// </summary>
    public ObservableCollection<SideMenuItem> SideMenuItems { get; }

    /// <summary>
    /// Gets the command used by UI elements to invoke navigation of the current selection.
    /// </summary>
    public ICommand MenuItemCommand { get; }

    /// <summary>
    /// Determines whether navigation is currently allowed based on selection and privileges.
    /// </summary>
    /// <returns>True if navigation can proceed; otherwise false.</returns>
    private bool CanNavigate()
        => _selectedMenuItem is not null && _privileges.CanAccess(_selectedMenuItem.Privilege);

    /// <summary>
    /// Performs navigation to the selected menu item's view if conditions allow
    /// and the target view differs from the currently displayed one.
    /// </summary>
    private void ExecuteNavigation()
    {
        var item = _selectedMenuItem;
        if (item is null) return;

        if (App.Current?.MainWindow is not MainWindow mw) return;
        var host = mw.MainContent;
        if (host is null) return;

        if (!ReferenceEquals(host.Content, item.View))
            host.Content = item.View;
    }

    /// <summary>
    /// Handles privilege level changes:
    ///  - Notifies bindings
    ///  - Updates command state
    ///  - Revalidates and adjusts current selection if necessary
    /// </summary>
    /// <param name="sender">Event source (ignored).</param>
    /// <param name="_">Unused event args.</param>
    private void OnPrivilegeLevelChanged(object? sender, System.EventArgs _)
    {
        OnPropertyChanged(nameof(CurrentLevel));
        OnPropertyChanged(nameof(SideMenuItems));
        _menuItemCommand.RaiseCanExecuteChanged();
        RefreshMenuVisibilityAndSelection();
    }

    /// <summary>
    /// Ensures the current selection remains valid for the active privilege level;
    /// selects the best alternative if it is no longer accessible.
    /// </summary>
    private void RefreshMenuVisibilityAndSelection()
    {
        // Refresh any bindings relying on collection view filtering/converters.
        CollectionViewSource.GetDefaultView(SideMenuItems)?.Refresh();

        var current = _selectedMenuItem;
        if (current is null || !IsItemVisibleForCurrentLevel(current))
        {
            var next = GetBestVisibleSelection();
            if (next is not null && !ReferenceEquals(next, current))
            {
                _selectedMenuItem = next;
                OnPropertyChanged(nameof(SelectedMenuItem));
                ExecuteNavigation();
                _menuItemCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Determines the next menu item to select after a logoff action.
    /// Preference order:
    ///  1. Login item (if visible)
    ///  2. First visible non-logoff item
    ///  3. First visible item (including logoff if unavoidable)
    ///  4. Login item (fallback)
    /// </summary>
    /// <returns>The post-logoff selection candidate.</returns>
    private SideMenuItem GetPostLogoffSelection()
    {
        if (IsItemVisibleForCurrentLevel(_loginItem)) return _loginItem;
        return GetFirstVisible(preferNonLogoff: true)
               ?? GetFirstVisible(preferNonLogoff: false)
               ?? _loginItem;
    }

    /// <summary>
    /// Computes an appropriate best default selection for the current privilege level.
    /// For guests, prefers the login item; otherwise first visible non-logoff item.
    /// </summary>
    /// <returns>A suitable visible item or null.</returns>
    private SideMenuItem? GetBestVisibleSelection()
    {
        if (CurrentLevel == PrivilegeLevel.Guest && IsItemVisibleForCurrentLevel(_loginItem))
            return _loginItem;

        return GetFirstVisible(preferNonLogoff: true)
               ?? GetFirstVisible(preferNonLogoff: false);
    }

    /// <summary>
    /// Retrieves the first visible item based on privilege visibility rules.
    /// Optionally skips logoff items on the first pass.
    /// </summary>
    /// <param name="preferNonLogoff">When true, logoff items are skipped.</param>
    /// <returns>The first matching visible item or null.</returns>
    private SideMenuItem? GetFirstVisible(bool preferNonLogoff)
    {
        foreach (var item in SideMenuItems)
        {
            if (preferNonLogoff && item.IsLogoff) continue;
            if (IsItemVisibleForCurrentLevel(item))
                return item;
        }
        return null;
    }

    /// <summary>
    /// Evaluates whether a menu item should be visible under current privilege conditions.
    /// Includes special-case filtering for login/logoff items.
    /// </summary>
    /// <param name="item">Item to evaluate.</param>
    /// <returns>True if visible; otherwise false.</returns>
    private bool IsItemVisibleForCurrentLevel(SideMenuItem item)
    {
        if (!_privileges.CanAccess(item.Privilege))
            return false;

        if (ReferenceEquals(item, _loginItem) && CurrentLevel != PrivilegeLevel.Guest)
            return false;

        if (ReferenceEquals(item, _logoffItem) && CurrentLevel == PrivilegeLevel.Guest)
            return false;

        return true;
    }
}
