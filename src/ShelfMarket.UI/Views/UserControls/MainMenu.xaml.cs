using System.Windows.Controls;

namespace ShelfMarket.UI.Views.UserControls;

/// <summary>
/// Interaction logic for MainMenu.xaml
/// </summary>
public partial class MainMenu : UserControl
{

    private MainWindow? _mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

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
        if (_mainWindow != null)
        {
            // Use ContentControl to host different views
            _mainWindow.MainContent.Content = new EanLabelGeneratorView();
        }
    }

    /* TODO : Delete below before final commit */
    // Added: placeholders for not-yet-implemented views
    private void MenuShelfTypes_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_mainWindow != null)
        {
            // Use ContentControl to host different views
            _mainWindow.MainContent.Content = new ManagesShelfTypeView();
        }
    }

    private void MenuShelves_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_mainWindow != null)
        {
            // Use ContentControl to host different views
            //_mainWindow.MainContent.Content = new ManagesShelfView();
        }
    }

    private void MenuTenants_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_mainWindow != null)
        {
            // Use ContentControl to host different views
            _mainWindow.MainContent.Content = new ManagesShelfTenantView();
        }
    }
    private void MenuShelfTenants_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_mainWindow != null)
        {
            // Use ContentControl to host different views
            _mainWindow.MainContent.Content = new ManagesShelfTenantView();
        }
    }
}
