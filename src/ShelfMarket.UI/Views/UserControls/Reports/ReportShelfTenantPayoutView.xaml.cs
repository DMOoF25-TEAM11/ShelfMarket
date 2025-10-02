using System.Windows.Controls;
using ShelfMarket.UI.ViewModels.Reports;

namespace ShelfMarket.UI.Views.UserControls.Reports;

///// <summary>
///// Interaction logic for ReportShelfTenantPayoutViewModel.xaml
///// </summary>
public partial class ReportShelfTenantPayoutView : UserControl
{
    public ReportShelfTenantPayoutView()
    {
        InitializeComponent();
        DataContext ??= new ReportShelfTenantPayoutViewModel();
    }
}
