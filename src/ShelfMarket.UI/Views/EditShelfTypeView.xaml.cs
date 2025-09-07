using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views;

/// <summary>
/// Interaction logic for EditShelfType.xaml
/// </summary>
public partial class EditShelfTypeView : Page
{
    private ShelfTypeListViewModel? _listVm;
    private ShelfTypeViewModel? _vm;

    public EditShelfTypeView()
    {
        InitializeComponent();

        ShelfTypeViewModel vm = new();
        DataContext = vm;
        _vm = vm;

        _listVm = App.HostInstance.Services.GetRequiredService<ShelfTypeListViewModel>();
        ShelfTypesListControl.DataContext = _listVm;

        if (_listVm is not null)
            _listVm.PropertyChanged += ListVm_OnPropertyChanged;

        //vm.ShelfTypeSaved += (_, __) =>
        //{
        //    if (_listVm is not null)
        //        _ = _listVm.RefreshAsync();
        //};

        Loaded += async (_, __) =>
        {
            if (_listVm is not null)
                await _listVm.RefreshAsync();
        };
    }
    private async void ListVm_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShelfTypeListViewModel.SelectedItem)
            && _listVm?.SelectedItem is { } item
            && _vm is not null
            && item.Id.HasValue)
        {
            await _vm.LoadAsync(item.Id.Value);
        }
    }
}
