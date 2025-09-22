using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.Domain.Enums;
using ShelfMarket.UI.Commands;
using ShelfMarket.UI.ViewModels.Abstracts;
using ShelfMarket.UI.Views.UserControls;

namespace ShelfMarket.UI.ViewModels;

public class SideMenuViewModel : ModelBase
{
    private readonly IPrivilegeService _privileges;

    public SideMenuViewModel()
    {
        _privileges = App.HostInstance.Services.GetRequiredService<IPrivilegeService>();
        MenuItemCommand = new RelayCommand(OnCommand, CanCommand);

        // Build items and compute visibility from privileges
        SideMenuItems = new List<SideMenuItem>
        {
            new SideMenuItem("Log på",          new ChangeUserView(),    PrivilegeLevel.Guest),
            new SideMenuItem("Log af",          new ShelfView(),         PrivilegeLevel.Guest, isLogoff: true),
            new SideMenuItem("Reoler",          new ShelfView(),         PrivilegeLevel.Guest),
            new SideMenuItem("Salg",            new SalesView(),         PrivilegeLevel.User),
            new SideMenuItem("Økonomi",         new FinanceView(),       PrivilegeLevel.Admin),
            new SideMenuItem("Arrangementer",   new EventsView(),        PrivilegeLevel.User),
            new SideMenuItem("Lejere",          new TenantView(),        PrivilegeLevel.User),
            new SideMenuItem("Vedligeholdelse", new ManagesShelfTypeView(), PrivilegeLevel.User)
        };
        foreach (var item in SideMenuItems)
            item.IsVisible = _privileges.CanAccess(item.Privilege);

        // Select first visible item by default
        Selected = SideMenuItems.FirstOrDefault(i => i.IsVisible);
    }

    public SideMenuItem? SelectedItem { get; set; }

    private SideMenuItem? _selected;
    // Bound by the ListBox
    public SideMenuItem? Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            OnPropertyChanged(nameof(Selected));

            // Quick logoff and go to ShelfView
            if (value?.IsLogoff == true)
            {
                var shelf = SideMenuItems.FirstOrDefault(i => i.Title == "Reoler") ?? SideMenuItems.First();
                _selected = shelf;
                OnPropertyChanged(nameof(Selected));
            }
        }
    }

    // Ensure PrivilegeService is set to Guest when selection changes to Logoff
    protected new void OnPropertyChanged(string? name = null)
    {
        base.OnPropertyChanged(name);
        if (name == nameof(Selected) && Selected?.IsLogoff == true)
        {
            _privileges.SignIn(PrivilegeLevel.Guest, null);
        }
    }

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
        public bool IsLogoff
        {
            get;
            init;
        }
        public bool IsVisible { get; set; } // used by BoolToVis
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
            OnCommand();
            if (MenuItemCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
        }
    }

    public List<SideMenuItem> SideMenuItems { get; }

    public ICommand MenuItemCommand { get; }

    private bool IsVisible(SideMenuItem item)
        => _privileges.CanAccess(item.Privilege);

    private bool CanCommand()
        => SelectedMenuItem is not null && _privileges.CanAccess(SelectedMenuItem.Privilege);

    private void OnCommand()
    {
        if (SelectedMenuItem is null) return;
        MainWindow? mainWindow = App.Current.MainWindow as MainWindow;
        if (mainWindow == null) return;
        mainWindow.CurrentView.Content = SelectedMenuItem.View;
        // Example: expose SelectedMenuItem.View to a host region in your main view.
        // This VM intentionally leaves navigation hookup to the shell.
    }
}
