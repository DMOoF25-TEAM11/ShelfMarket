using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstracts.Services;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.UserControls;

public partial class EanLabelGeneratorView : UserControl
{
    public EanLabelGeneratorView()
    {
        InitializeComponent();

        // Resolve service(s) from app DI and set DataContext with MVVM VM.
        var sp = App.HostInstance.Services;
        var barcode = sp.GetRequiredService<IEan13BarCode>();
        DataContext = new EanLabelGeneratorViewModel(barcode);
    }
}