using System;
using System.Windows;
using System.Windows.Controls;

namespace ShelfMarket.UI.Views.Popups
{
    /// <summary>
    /// Popup vindue til at tilføje nye reoler til systemet
    /// </summary>
    public partial class AddShelfPopup : UserControl
    {
        public event EventHandler? ReolTilfoejet;
        public event EventHandler? Annulleret;

        public AddShelfPopup()
        {
            InitializeComponent();
        }

        private void TilfoejReol_Click(object sender, RoutedEventArgs e)
        {
            if (ValiderInput())
            {
                FejlTekst.Visibility = Visibility.Collapsed;
                ReolTilfoejet?.Invoke(this, EventArgs.Empty);
                LukPopup();
            }
        }

        private void Annuller_Click(object sender, RoutedEventArgs e)
        {
            Annulleret?.Invoke(this, EventArgs.Empty);
            LukPopup();
        }

        private bool ValiderInput()
        {
            if (!int.TryParse(ReolNummerTextBox.Text, out int reolNummer))
            {
                VisFejl("Reol nummer skal være et gyldigt tal");
                return false;
            }

            if (ErNummerIForvejen(reolNummer))
            {
                VisFejl("Dette nummer er allerede i brug");
                return false;
            }
                      

            return true;
        }

        private bool ErNummerIForvejen(int nummer)
        {
            int[] brugteNumre = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 20, 25 };
            return Array.Exists(brugteNumre, x => x == nummer);
        }

        private void VisFejl(string fejlbesked)
        {
            FejlTekst.Text = fejlbesked;
            FejlTekst.Visibility = Visibility.Visible;
        }

        private void LukPopup()
        {
            Annulleret?.Invoke(this, EventArgs.Empty);
        }

        public void Nulstil()
        {
            ReoltypeComboBox.SelectedIndex = 0;
            OrienteringComboBox.SelectedIndex = 0;
            ReolNummerTextBox.Text = "1";
            FejlTekst.Visibility = Visibility.Collapsed;
        }
    }
}


