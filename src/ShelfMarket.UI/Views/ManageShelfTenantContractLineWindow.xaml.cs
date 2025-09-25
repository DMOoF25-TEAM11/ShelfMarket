using System.Windows;
using ShelfMarket.UI.Views.UserControls;

namespace ShelfMarket.UI.Views;

public partial class ManageShelfTenantContractLineWindow : Window
{
    public ManageShelfTenantContractLineWindow(int contractId)
    {
        InitializeComponent();
        Loaded += (_, __) =>
        {
            var page = new ManageShelfTenantContractLineView();
            page.DataContext = DataContext;
            ContentFrame.Navigate(page);
        };
    }

    public void SetDataContext(object vm) => DataContext = vm;
}