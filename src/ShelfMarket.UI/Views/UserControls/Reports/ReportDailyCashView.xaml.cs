using System.Windows.Controls;
using ShelfMarket.UI.ViewModels.Reports;

namespace ShelfMarket.UI.Views.UserControls.Reports;

/// <summary>
/// Interaction logic for ReportDailyCashView.xaml
/// </summary>
public partial class ReportDailyCashView : UserControl
{
    public ReportDailyCashView()
    {
        InitializeComponent();
        var vm = new ReportDailyCashViewModel(); // or resolve via DI

        DataContext ??= vm;
    }
}
