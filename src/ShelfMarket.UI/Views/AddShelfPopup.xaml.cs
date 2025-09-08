using System;
using System.Windows;
using System.Windows.Controls;

namespace ShelfMarket.UI.Views
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

        /// <summary>
        /// Håndterer klik på Tilføj reol knappen
        /// </summary>
        private void TilfoejReol_Click(object sender, RoutedEventArgs e)
        {
            // Valider input
            if (ValiderInput())
            {
                // Her ville man normalt tilføje reolen til databasen
                // For nu simulerer vi bare en succesfuld tilføjelse
                
                // Skjul fejltekst hvis den er synlig
                FejlTekst.Visibility = Visibility.Collapsed;
                
                // Trigger event for at notificere parent om tilføjelse
                ReolTilfoejet?.Invoke(this, EventArgs.Empty);
                
                // Luk popup vinduet
                LukPopup();
            }
        }

        /// <summary>
        /// Håndterer klik på Annuller knappen
        /// </summary>
        private void Annuller_Click(object sender, RoutedEventArgs e)
        {
            Annulleret?.Invoke(this, EventArgs.Empty);
            LukPopup();
        }

        /// <summary>
        /// Validerer brugerens input
        /// </summary>
        private bool ValiderInput()
        {
            // Tjek om reol nummer er et gyldigt tal
            if (!int.TryParse(ReolNummerTextBox.Text, out int reolNummer))
            {
                VisFejl("Reol nummer skal være et gyldigt tal");
                return false;
            }

            // Tjek om nummeret allerede er i brug (simuleret)
            if (ErNummerIForvejen(reolNummer))
            {
                VisFejl("Dette nummer er allerede i brug");
                return false;
            }

            // Tjek om nummeret er inden for gyldigt område
            if (reolNummer < 1 || reolNummer > 80)
            {
                VisFejl("Reol nummer skal være mellem 1 og 80");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Simulerer tjek for om et nummer allerede er i brug
        /// I en rigtig applikation ville dette tjekke mod databasen
        /// </summary>
        private bool ErNummerIForvejen(int nummer)
        {
            // Simulerer at nummer 1-13 og 15, 20, 25 allerede er i brug
            int[] brugteNumre = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 20, 25 };
            return Array.Exists(brugteNumre, x => x == nummer);
        }

        /// <summary>
        /// Viser fejltekst til brugeren
        /// </summary>
        private void VisFejl(string fejlbesked)
        {
            FejlTekst.Text = fejlbesked;
            FejlTekst.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Lukker popup vinduet
        /// </summary>
        private void LukPopup()
        {
            // Trigger Annulleret event så MainWindow kan lukke overlay'en
            Annulleret?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Nulstiller popup vinduet til standard værdier
        /// </summary>
        public void Nulstil()
        {
            ReoltypeComboBox.SelectedIndex = 0;
            OrienteringComboBox.SelectedIndex = 0;
            ReolNummerTextBox.Text = "1";
            FejlTekst.Visibility = Visibility.Collapsed;
        }
    }
}
