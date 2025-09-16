using System.Windows;
using System.Windows.Controls;

namespace ShelfMarket.UI.Views.Windows;

public partial class ChooseShelfTypeWindow : UserControl
{
    public ChooseShelfTypeWindow()
    {
        InitializeComponent();
    }

    public void SetShelfNumber(int number)
    {
        TitleText.Text = $"Reol ({number})";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
        if (overlay != null)
            overlay.Visibility = Visibility.Collapsed;
    }

    private void Choose_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
        if (overlay != null)
            overlay.Visibility = Visibility.Collapsed;
    }
}


