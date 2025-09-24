using System.Windows.Controls;
using ShelfMarket.UI.ViewModels;
using ShelfMarket.UI.ViewModels.List;

namespace ShelfMarket.UI.Views;

/// <summary>
/// Interaction logic for ManagesShelfRentContractView.xaml
/// </summary>
public partial class ManagesShelfTanentContractView : Page
{
    private ManagesShelfTanentContractViewModel? _vm;
    private ManagesShelfTanentContractListViewModel? _listVm;

    public ManagesShelfTanentContractView()
    {
        InitializeComponent();

        ManagesShelfTanentContractViewModel vm = new();
        DataContext = vm;
        _vm = vm;


        // Refresh the list when an entity is saved
        vm.EntitySaved += (_, __) =>
        {
            if (_listVm is not null)
                _ = _listVm.RefreshAsync();
        };

        // Initial load of the list
        Loaded += async (_, __) =>
        {
            if (_listVm is not null)
                await _listVm.RefreshAsync();
        };
    }
    private async void ListVm_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ManagesShelfTanentContractListViewModel.SelectedItem)
            && _listVm?.SelectedItem is { } item
            && _vm is not null
            && item.Id.HasValue)
        {
            await _vm.LoadAsync(item.Id.Value);
        }
    }
}
