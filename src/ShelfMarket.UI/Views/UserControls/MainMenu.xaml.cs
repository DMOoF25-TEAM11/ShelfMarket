using System.Windows.Controls;

namespace ShelfMarket.UI.Views.UserControls;

/// <summary>
/// Interaction logic for MainMenu.xaml
/// </summary>
public partial class MainMenu : UserControl
{
    public MainMenu()
    {
        InitializeComponent();
    }

    private void MenuFil_Exit(object sender, System.Windows.RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void MenuViews_ShelfType(object sender, System.Windows.RoutedEventArgs e)
    {
        //var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        //mainWindow?.MainFrame.Navigate(new EditShelfTypeView());
    }

    private void MenuEan_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        if (mainWindow != null)
        {
            // Use ContentControl to host different views
            mainWindow.Content = new EanLabelGeneratorView();
            mainWindow.Title = "Reolmarkedet - EAN Label Generator";
            mainWindow.PageTitle.Text = "EAN Label Generator";
        }
    }
}
