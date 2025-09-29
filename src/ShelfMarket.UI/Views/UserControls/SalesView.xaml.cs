using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.UserControls;

/// <summary>
/// Interaction logic for SalesView.xaml
/// </summary>
public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();

        DataContext = new SalesViewModel();

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

    private void ReolTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Ensure the latest value is pushed to the ViewModel
            var binding = ReolTextBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();

            if (DataContext is SalesViewModel vm)
            {
                vm.OnShelfNumberEnteredCommand.Execute(null);
                if (vm.ShelfNumber != string.Empty)
                {
                    PriceTextBox.Focus();
                    PriceTextBox.SelectAll();
                }
            }
        }
    }
}
