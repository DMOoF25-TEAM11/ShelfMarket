using System.Windows.Controls;
using ShelfMarket.UI.Views.UserControls.Reports;

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

    private void MenuEan_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_mainWindow != null)
        {
            _mainWindow.MainContent.Content = new EanLabelGeneratorView();
        }
    }

    private void MenuCashReport_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_mainWindow != null)
        {
            _mainWindow.MainContent.Content = new ReportDailyCashView();
        }
    }

    private void MenuShelfTenantPayoutReport_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_mainWindow != null)
        {
            _mainWindow.MainContent.Content = new ReportShelfTenantPayoutView();
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
