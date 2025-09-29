using System.Windows.Controls;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views.UserControls
{
    /// <summary>
    /// Interaction logic for SalesLineListView.xaml
    /// </summary>
    public partial class SalesLineListView : UserControl
    {
        public SalesLineListView()
        {
            InitializeComponent();

            DataContext = new SalesLineViewModel();
        }
    }
}
