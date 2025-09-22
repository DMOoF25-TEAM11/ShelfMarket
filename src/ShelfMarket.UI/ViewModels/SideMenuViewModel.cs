using System.Collections.ObjectModel;
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
        SideMenuItems = new ObservableCollection<SideMenuItem>
        {
            new SideMenuItem("Log på", new LoginView(), PrivilegeLevel.Guest),
            new SideMenuItem("Log af", new ShelfView(), PrivilegeLevel.Guest, isLogoff: true),
            new SideMenuItem("Reoler", new ShelfView(), PrivilegeLevel.Guest),
            new SideMenuItem("Salg", new SalesView(), PrivilegeLevel.User),
            new SideMenuItem("Økonomi", new FinanceView(), PrivilegeLevel.Admin),
            new SideMenuItem("Arrangementer", new EventsView(), PrivilegeLevel.User),
            new SideMenuItem("Lejere", new TenantView(), PrivilegeLevel.User),
            new SideMenuItem("Vedligeholdelse", new ManagesShelfTypeView(), PrivilegeLevel.User)
        };

        SelectedMenuItem ??= SideMenuItems[2];
    }


    // Deprecated: left in place to avoid breaking references; no longer used by the view binding.
    //private SideMenuItem? _selected;
    //public SideMenuItem? Selected
    //{
    //    get => _selected;
    //    set
    //    {
    //        if (_selected == value) return;
    //        _selected = value;
    //        OnCommand();
    //        OnPropertyChanged(nameof(Selected));
    //    }
    //}

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

            // Quick logoff and go to ShelfView
            if (value?.IsLogoff == true)
            {
                _privileges.SignIn(PrivilegeLevel.Guest, null);
                var shelf = SideMenuItems.FirstOrDefault(i => i.Title == "Reoler") ?? SideMenuItems.FirstOrDefault();
                if (shelf != null)
                {
                    _selectedMenuItem = shelf;
                    OnPropertyChanged(nameof(SelectedMenuItem));
                }
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
        //mainWindow.MainContent.Content = SelectedMenuItem.View;
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
    }
}
