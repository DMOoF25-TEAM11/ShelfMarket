using System.Windows.Controls;
using ShelfMarket.Application.Abstract;
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
        ISalesRepository? repo = App.HostInstance.Services.GetService(typeof(ISalesRepository)) as ISalesRepository
            ?? throw new InvalidOperationException("ISalesRepository service is not registered.");
        var vm = new ReportDailyCashViewModel(repo);

        DataContext ??= vm;
    }
}
