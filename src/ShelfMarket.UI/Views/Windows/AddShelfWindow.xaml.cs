using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstracts.Services;
using ShelfMarket.Infrastructure.Persistence;

namespace ShelfMarket.UI.Views.Windows
{
    /// <summary>
    /// Popup vindue til at tilføje nye reoler til systemet
    /// </summary>
    public partial class AddShelfWindow : UserControl
    {
        public event EventHandler? ReolTilfoejet;
        public event EventHandler? Annulleret;

        public AddShelfWindow()
        {
            InitializeComponent();
        }

        private void TilfoejReol_Click(object sender, RoutedEventArgs e)
        {
            _ = CreateAsync();
        }

        private void Annuller_Click(object sender, RoutedEventArgs e)
        {
            Annulleret?.Invoke(this, EventArgs.Empty);
            LukPopup();
        }

        private async System.Threading.Tasks.Task CreateAsync()
        {
            if (!int.TryParse(ReolNummerTextBox.Text, out int reolNummer))
            {
                VisFejl("Reol nummer skal være et gyldigt tal");
                return;
            }

            bool horizontal = OrienteringComboBox.SelectedIndex == 1; // 0=Lodret, 1=Vandret

            try
            {
                using var scope = App.HostInstance.Services.CreateScope();
                var layout = scope.ServiceProvider.GetRequiredService<IShelfLayoutService>();
                // Ensure a default shelf type exists if needed
                var db = scope.ServiceProvider.GetRequiredService<ShelfMarketDbContext>();
                var type = db.ShelfTypes.FirstOrDefault();
                if (type == null)
                {
                    db.ShelfTypes.Add(new Domain.Entities.ShelfType { Id = Guid.NewGuid(), Name = "Default" });
                    await db.SaveChangesAsync();
                }

                // Spawn på X=22, Y=0
                var ok = await layout.TryCreateShelfAsync(reolNummer, horizontal, 22, 0);
                if (!ok)
                {
                    VisFejl("Nummer findes allerede eller feltet (22,0) er optaget.");
                    return;
                }

                FejlTekst.Visibility = Visibility.Collapsed;
                ReolTilfoejet?.Invoke(this, EventArgs.Empty);
                LukPopup();
            }
            catch (Exception ex)
            {
                VisFejl($"Der opstod en fejl: {ex.Message}");
            }
        }

        // Nummer-check håndteres i service/repo – lokal liste er fjernet

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


