using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services.Barcodes;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.UserControls;

public partial class EanLabelGeneratorView : UserControl
{
    public EanLabelGeneratorView()
    {
        InitializeComponent();

        // Resolve service(s) from app DI and set DataContext with MVVM VM.
        var sp = App.HostInstance.Services;
        var barcode = sp.GetRequiredService<IEan13Generator>();
        DataContext = new EanLabelGeneratorViewModel(barcode);
    }
}