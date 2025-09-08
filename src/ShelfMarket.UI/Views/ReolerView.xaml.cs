using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ShelfMarket.UI.Views
{
    /// <summary>
    /// View til at vise og administrere reoler i lageret
    /// </summary>
    public partial class ReolerView : UserControl
    {
        public ReolerView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Håndterer klik på "Tilføj ny reol" knappen
        /// </summary>
        private void TilfoejNyReol_Click(object sender, RoutedEventArgs e)
        {
            // Find MainWindow og dens overlay komponenter
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var overlay = mainWindow.FindName("PopupOverlay") as Grid;
                var popupContent = mainWindow.FindName("AddShelfPopupContent") as AddShelfPopup;
                
                if (overlay != null && popupContent != null)
                {
                    // Opsæt event handlers når popup åbnes
                    popupContent.ReolTilfoejet += OnReolTilfoejet;
                    popupContent.Annulleret += OnPopupAnnulleret;
                    
                    // Nulstil popup vinduet til standard værdier
                    popupContent.Nulstil();
                    
                    // Vis overlay
                    overlay.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Håndterer når en ny reol er tilføjet
        /// </summary>
        private void OnReolTilfoejet(object? sender, EventArgs e)
        {
            // Her ville man normalt opdatere UI'en med den nye reol
            // For nu viser vi bare en simpel besked
            MessageBox.Show("Reol er tilføjet succesfuldt!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Luk overlay
            LukOverlay();
        }

        /// <summary>
        /// Håndterer når popup vinduet annulleres
        /// </summary>
        private void OnPopupAnnulleret(object? sender, EventArgs e)
        {
            // Luk overlay uden at tilføje reol
            LukOverlay();
        }

        /// <summary>
        /// Lukker overlay vinduet
        /// </summary>
        private void LukOverlay()
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
            var popupContent = mainWindow?.FindName("AddShelfPopupContent") as AddShelfPopup;
            
            if (overlay != null)
            {
                overlay.Visibility = Visibility.Collapsed;
            }
            
            // Unsubscribe fra events for at undgå memory leaks
            if (popupContent != null)
            {
                popupContent.ReolTilfoejet -= OnReolTilfoejet;
                popupContent.Annulleret -= OnPopupAnnulleret;
            }
        }
    }
}
