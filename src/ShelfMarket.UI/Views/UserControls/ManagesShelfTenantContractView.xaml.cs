using System.Windows;
using System.Windows.Controls;
using ShelfMarket.Domain.Entities;

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
}
