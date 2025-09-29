using System.Windows.Controls;

namespace ShelfMarket.UI.Views.UserControls.Reports;

/// <summary>
/// Interaction logic for ReportDailyCashView.xaml
/// </summary>
public partial class ReportDailyCashView : UserControl
{
    public ReportDailyCashView()
    {
        InitializeComponent();
        DataContext ??= new ViewModels.Reports.ReportDailyCashViewModel();
    }
}
