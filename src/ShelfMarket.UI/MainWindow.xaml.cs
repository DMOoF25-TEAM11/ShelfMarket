using System.Windows;
using ShelfMarket.UI.Views.UserControls;

namespace ShelfMarket.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainMenu.Content = new MainMenu();
        SideMenu.Content = new SideMenu();
    }
}