using System.Windows;
using System.Windows.Controls;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.UserControls;

/// <summary>
/// Interaction logic for ManageShelfTenantContractLineView.xaml
/// </summary>
public partial class ManageShelfTenantContractLineView : UserControl
{
    public ManageShelfTenantContractLineView()
    {
        InitializeComponent();

        DataContext ??= new ManageShelfTenantContractLineViewModel();

        Loaded += ManageShelfTenantContractLineView_Loaded;
    }

    private void ManageShelfTenantContractLineView_Loaded(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mw)
        {
            if (mw.FindName("PageTitle") is TextBlock title)
            {
                title.Text = "Tilføj reoler til kontrakten";
            }
        }
    }
}