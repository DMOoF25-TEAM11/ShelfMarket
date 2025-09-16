using System.Windows.Controls;
using ShelfMarket.UI.ViewModels;

namespace ShelfMarket.UI.Views
{
    /// <summary>
    /// Interaction logic for ManagesShelfTanentContractLineView.xaml
    /// </summary>
    public partial class ManagesShelfTanentContractLineView : Page
    {
        private ManagesShelfTanentContractLineViewModel? _vm;
        private ManagesShelfTanentContractLineListViewModel? _listVm;

        public ManagesShelfTanentContractLineView()
        {
            InitializeComponent();

            ManagesShelfTanentContractLineViewModel vm = new();
            DataContext = vm;
            _vm = vm;
        }
    }
}
