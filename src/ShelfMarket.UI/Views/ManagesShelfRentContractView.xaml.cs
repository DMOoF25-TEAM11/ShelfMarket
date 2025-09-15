using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views;

/// <summary>
/// Interaction logic for ManagesShelfRentContractView.xaml
/// </summary>
public partial class ManagesShelfRentContractView : Page
{
    private ManagesShelfTanentContractViewModel? _vm;
    private ManagesShelfTanentContractListViewModel? _listVm;

    public ManagesShelfRentContractView()
    {
        InitializeComponent();

        ManagesShelfTanentContractViewModel vm = new();
        DataContext = vm;
        _vm = vm;

        _listVm = App.HostInstance.Services.GetRequiredService<ManagesShelfTanentContractListViewModel>();

    }
}
