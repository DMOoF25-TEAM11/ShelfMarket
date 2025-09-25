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
/// ViewModel controlling the side navigation menu behavior:
/// - Maintains menu items and current selection
/// - Enforces privilege based visibility and access
/// - Handles login / logoff transitions
/// - Navigates the main window content area to the selected view
/// </summary>
/// <remarks>
/// Optimizations:
/// - Caches references to special items (login/logoff/default)
/// - Avoids redundant navigation when the target view is already displayed
/// - Uses direct loops over LINQ for lower overhead (predictable small collection)
/// - Reuses instantiated UserControls
/// </remarks>
public sealed class SideMenuViewModel : ModelBase
{
    private readonly IPrivilegeService _privileges;
    private readonly RelayCommand _menuItemCommand;

    // Cached sentinel items for faster comparison and logic branching
    private readonly SideMenuItem _loginItem;
    private readonly SideMenuItem _logoffItem;
    private readonly SideMenuItem _defaultItem; // "Reoler"

    /// <summary>
    /// Initializes a new instance of the <see cref="SideMenuViewModel"/> class,
    /// wires privilege change notifications, builds the menu, and selects the default item.
    /// </summary>
    public SideMenuViewModel()
    {
        _privileges = App.HostInstance.Services.GetRequiredService<IPrivilegeService>();
        _privileges.CurrentLevelChanged += OnPrivilegeLevelChanged;

        // Pre-create views once (reuse instances instead of recreating per navigation)
        _loginItem = new SideMenuItem("Log på", new LoginView(), PrivilegeLevel.Guest);
        _logoffItem = new SideMenuItem("Log af", new ShelfView(), PrivilegeLevel.Guest, isLogoff: true);
        _defaultItem = new SideMenuItem("Reoler", new ShelfView(), PrivilegeLevel.Guest);

        // Populate (order preserved – bindings may rely on it)
        SideMenuItems =
        [
            _loginItem,
            _logoffItem,
            _defaultItem,
            new("Salg",            new SalesView(),                 PrivilegeLevel.User),
            new("Økonomi",         new FinanceView(),               PrivilegeLevel.Admin),
            new("Arrangementer",   new EventsView(),                PrivilegeLevel.User),
            new("Lejere",          new ManagesShelfTenantView(),    PrivilegeLevel.User),
            new("Vedligeholdelse", new MaintenanceView(),           PrivilegeLevel.User)
        ];

        _menuItemCommand = new RelayCommand(ExecuteNavigation, CanNavigate);
        MenuItemCommand = _menuItemCommand;

        _selectedMenuItem = _defaultItem;
        OnPropertyChanged(nameof(SelectedMenuItem));

        // Immediate attempt (works if MainWindow already created)
        ExecuteNavigation();

        // Deferred attempt (covers case where MainWindow not yet ready at construction time)
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            new Action(() =>
            {
                if (ReferenceEquals(_selectedMenuItem, _defaultItem))
                    ExecuteNavigation();
            }),
            DispatcherPriority.Loaded);
    }

    public PrivilegeLevel CurrentLevel => _privileges.CurrentLevel;

    public sealed class SideMenuItem
    {
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
        /// Indicates whether this item triggers logout logic.
        /// </summary>
        public bool IsLogoff { get; }

        /// <summary>
        /// Optional cached visibility flag (not actively used; available for future virtualization or pre-filtering).
        /// </summary>
        public bool IsVisible { get; set; }
    }

    private SideMenuItem? _selectedMenuItem;

    /// <summary>
    /// Gets or sets the currently selected menu item.
    /// Setting this property performs navigation or logout logic as appropriate.
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
                // Sign out and redirect to a valid item without re-processing the logoff item again
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
    /// Gets the collection of all available menu items (visibility may be determined in bindings via converters).
    /// </summary>
    public ObservableCollection<SideMenuItem> SideMenuItems { get; }

    /// <summary>
    /// Gets the command used by the UI to invoke navigation for the selected menu item.
    /// </summary>
    public ICommand MenuItemCommand { get; }

    /// <summary>
    /// Determines whether navigation is currently allowed based on selection and privileges.
    /// </summary>
    /// <returns>True if navigation can proceed; otherwise false.</returns>
    private bool CanNavigate()
        => _selectedMenuItem is not null && _privileges.CanAccess(_selectedMenuItem.Privilege);

    /// <summary>
    /// Performs navigation to the selected menu item's view if conditions allow and the target differs from current content.
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
    /// Handles privilege level transitions:
    /// - Raises property notifications
    /// - Updates command state
    /// - Reconciles selection if current item becomes inaccessible
    /// </summary>
    private void OnPrivilegeLevelChanged(object? sender, System.EventArgs _)
    {
        OnPropertyChanged(nameof(CurrentLevel));
        OnPropertyChanged(nameof(SideMenuItems)); // Maintain original contract for potential bindings
        _menuItemCommand.RaiseCanExecuteChanged();
        RefreshMenuVisibilityAndSelection();
    }

    /// <summary>
    /// Ensures the current selection is valid for the active privilege state; chooses a fallback if necessary.
    /// </summary>
    private void RefreshMenuVisibilityAndSelection()
    {
        // Force an ItemsControl to re-evaluate visibility bindings if using converters.
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
    /// Determines the selection to apply immediately after a logoff action.
    /// </summary>
    /// <returns>The next menu item to select.</returns>
    private SideMenuItem GetPostLogoffSelection()
    {
        if (IsItemVisibleForCurrentLevel(_loginItem)) return _loginItem;
        return GetFirstVisible(preferNonLogoff: true)
               ?? GetFirstVisible(preferNonLogoff: false)
               ?? _loginItem;
    }

    /// <summary>
    /// Computes an appropriate default selection for the current privilege state.
    /// </summary>
    /// <returns>The best visible menu item or null if none found.</returns>
    private SideMenuItem? GetBestVisibleSelection()
    {
        if (CurrentLevel == PrivilegeLevel.Guest && IsItemVisibleForCurrentLevel(_loginItem))
            return _loginItem;

        return GetFirstVisible(preferNonLogoff: true)
               ?? GetFirstVisible(preferNonLogoff: false);
    }

    /// <summary>
    /// Returns the first visible item, optionally preferring non-logoff entries.
    /// </summary>
    /// <param name="preferNonLogoff">If true, skip logoff items during the search.</param>
    /// <returns>The first matching item or null.</returns>
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
    /// Determines whether a menu item should currently be visible given privilege state and special-case rules.
    /// </summary>
    /// <param name="item">The item to evaluate.</param>
    /// <returns>True if the item is visible; otherwise false.</returns>
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
