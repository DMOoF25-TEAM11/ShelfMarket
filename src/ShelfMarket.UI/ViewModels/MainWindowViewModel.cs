using System.Windows.Controls;
using ShelfMarket.UI.ViewModels.Abstracts;

namespace ShelfMarket.UI.ViewModels;

public class MainWindowViewModel : ModelBase
{
    public UserControl? CurrentView { get; set; }
    public MainWindowViewModel()
    {
        // Default til Reoler som forside
    }
}


