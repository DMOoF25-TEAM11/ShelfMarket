using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShelfMarket.Domain.Entities;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views;

/// <summary>
/// Interaction logic for ManagesShelfRentContractView.xaml
/// </summary>
public partial class ManagesShelfTenantContractView : UserControl
{
    public ShelfTenant? ShelfTenant { get; private set; }

    // Parameterless constructor required for XAML/designer
    public ManagesShelfTenantContractView()
    {
        Initialize();
    }

    // Optional overload for programmatic creation with the required id
    public ManagesShelfTenantContractView(ShelfTenant shelfTenant)
    {
        ShelfTenant = shelfTenant;
        Initialize();
    }

    public void Initialize()
    {
        InitializeComponent();

        // Ensure runtime DataContext
        DataContext ??= new ViewModels.ManagesShelfTenantContractViewModel(ShelfTenant!);

        Loaded += ManagesTenantContractView_Loaded;

        Loaded += (_, __) =>
        {
            if (DataContext is ManagesShelfTenantContractViewModel vm)
            {
                vm.ContractCreated += (_, e) =>
                {
                    var win = new ManageShelfTenantContractLineWindow(e.ContractId)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    win.ShowDialog();
                };
            }
        };
    }

    private void ManagesTenantContractView_Loaded(object sender, RoutedEventArgs e)
    {
        // Set the MainWindow header text (TextBlock x:Name="PageTitle")
        if (System.Windows.Application.Current.MainWindow is MainWindow mw)
        {
            if (mw.FindName("PageTitle") is TextBlock title)
            {
                title.Text = "Reol lejerns kontrakter";
            }
        }
    }

    private void ContractNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Any(ch => !char.IsDigit(ch)))
            e.Handled = true;
    }

    private void ManagesShelfTenantContractView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ManagesShelfTenantContractViewModel oldVm)
            oldVm.ContractCreated -= Vm_ContractCreated;

        if (e.NewValue is ManagesShelfTenantContractViewModel newVm)
            newVm.ContractCreated += Vm_ContractCreated;
    }

    private void Vm_ContractCreated(object? sender, ContractCreatedEventArgs e)
    {
        var window = new ManageShelfTenantContractLineWindow(e.ContractId)
        {
            Owner = Window.GetWindow(this)
        };

        // Create and pass the contract id to the child VM
        var childVm = new ManagesShelfTanentContractLineViewModel
        {
            ContractId = e.ContractId
        };

        window.SetDataContext(childVm);
        window.ShowDialog();
    }
}
