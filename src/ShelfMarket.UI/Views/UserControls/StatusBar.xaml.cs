using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShelfMarket.UI.Views.UserControls
{
    /// <summary>
    /// Interaction logic for StatusBar.xaml
    /// </summary>
    public partial class StatusBar : UserControl
    {
        public StatusBar()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty IsInternetAvailableProperty = DependencyProperty.Register(
            nameof(IsInternetAvailable), typeof(bool), typeof(StatusBar), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether an active internet connection is available.
        /// </summary>
        public bool IsInternetAvailable
        {
            get => (bool)GetValue(IsInternetAvailableProperty);
            set => SetValue(IsInternetAvailableProperty, value);
        }

        
        public static readonly DependencyProperty IsDatabaseConnectedProperty = DependencyProperty.Register(
            nameof(IsDatabaseConnected), typeof(bool), typeof(StatusBar), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the database connection is currently active.
        /// </summary>
        public bool IsDatabaseConnected
        {
            get => (bool)GetValue(IsDatabaseConnectedProperty);
            set => SetValue(IsDatabaseConnectedProperty, value);
        }
    }
}
