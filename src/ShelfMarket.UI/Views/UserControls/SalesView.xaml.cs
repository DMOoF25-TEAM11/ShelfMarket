using System.Windows;
using System.Windows.Controls;

namespace ShelfMarket.UI.Views.UserControls;

/// <summary>
/// Interaction logic for SalesView.xaml
/// </summary>
public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();

        Loaded += SalesView_Loaded;
    }

    private void SalesView_Loaded(object sender, RoutedEventArgs e)
    {
        // Set the MainWindow header text (TextBlock x:Name="PageTitle")
        if (System.Windows.Application.Current.MainWindow is MainWindow mw)
        {
            if (mw.FindName("PageTitle") is TextBlock title)
            {
                title.Text = "Salg";
            }
        }
    }
}
