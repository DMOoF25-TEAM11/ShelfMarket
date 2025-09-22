using System.Windows.Controls;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.UserControls;

public partial class ChangeUserView : UserControl
{
    public ChangeUserView()
    {
        InitializeComponent();
        DataContext ??= new ChangeUserViewModel();
    }

    private void PasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ChangeUserViewModel vm && sender is PasswordBox pb)
        {
            vm.Password = pb.Password;
        }
    }
}