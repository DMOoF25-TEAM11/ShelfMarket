using System.Windows;
using System.Windows.Controls;

namespace ShelfMarket.UI.Views.Popups;

public partial class AddContractPopup : UserControl
{
    public AddContractPopup()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
        if (overlay != null)
        {
            overlay.Visibility = Visibility.Collapsed;
        }
    }

    private void ChooseShelves_Click(object sender, RoutedEventArgs e)
    {
        // Luk overlay og tilbage til kortet, så brugeren kan vælge reoler
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
        if (overlay != null)
        {
            overlay.Visibility = Visibility.Collapsed;
        }
        // Eventuel ekstra logik kan tilføjes her (gem midlertidig form state i VM)
    }
}
