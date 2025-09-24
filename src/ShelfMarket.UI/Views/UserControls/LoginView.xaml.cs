using System.Windows;
using System.Windows.Controls;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.UserControls;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        Loaded += LoginView_Loaded;
    }

    private void LoginView_Loaded(object sender, RoutedEventArgs e)
    {
        // Set the MainWindow header text (TextBlock x:Name="PageTitle")
        if (System.Windows.Application.Current.MainWindow is MainWindow mw)
        {
            if (mw.FindName("PageTitle") is TextBlock title)
            {
                title.Text = "Log på";
            }
        }
    }

    private void PasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is LoginRoleViewModel vm && sender is PasswordBox pb)
        {
            vm.Password = pb.Password;
        }
    }
}