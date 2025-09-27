using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.UserControls;

public partial class ManagesShelfTenantView : UserControl
{
    public ManagesShelfTenantView()
    {
        InitializeComponent();

        // Ensure runtime DataContext
        DataContext ??= App.HostInstance.Services.GetRequiredService<ManagesShelfTenantViewModel>();

        Loaded += ManagesTenantView_Loaded;
    }

    /// <summary>
    /// When the Tenants page is shown, set the header and refresh the list so any
    /// changes from other popups (e.g., new contracts creating tenants) are visible.
    /// </summary>
    private void ManagesTenantView_Loaded(object sender, RoutedEventArgs e)
    {
        // Set the MainWindow header text (TextBlock x:Name="PageTitle")
        if (System.Windows.Application.Current.MainWindow is MainWindow mw)
        {
            if (mw.FindName("PageTitle") is TextBlock title)
            {
                title.Text = "Lejere"; // change to any text you need
            }
        }

        // Always refresh when navigating to this view, so new tenants/contracts are reflected
        if (DataContext is ManagesShelfTenantViewModel vm)
        {
            _ = vm.RefreshAsync();
        }
    }
}