using System.Windows;
using System.Windows.Controls;

namespace ShelfMarket.UI.Views.Windows;

public partial class ShelfInfoWindow : UserControl
{
    public ShelfInfoWindow()
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
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
        if (overlay != null)
        {
            overlay.Visibility = Visibility.Collapsed;
        }
    }
}
