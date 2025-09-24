using System.Windows;
using System.Windows.Controls;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.UserControls;

public partial class ManagesShelfTenantView : UserControl
{
    public ManagesShelfTenantView()
    {
        InitializeComponent();

        // Ensure runtime DataContext
        DataContext ??= new ManagesShelfTenantViewModel();

        Loaded += ManagesTenantView_Loaded;
    }

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
    }

    //private async void ListVm_OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName == nameof(ManagesTenantListViewModel.SelectedItem)
    //        && _listVm?.SelectedItem is { } item
    //        && _vm is not null
    //        && item.Id.HasValue)
    //    {
    //        await _vm.LoadAsync(item.Id.Value);
    //    }
    //}
}