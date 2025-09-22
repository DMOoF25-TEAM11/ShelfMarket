using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;

namespace ShelfMarket.UI.Views.Windows;

public partial class ShelfInfoWindow : UserControl
{
    private int _shelfNumber;

    public ShelfInfoWindow()
    {
        InitializeComponent();
    }

    public void SetShelfNumber(int number)
    {
        _shelfNumber = number;
        if (TitleText != null)
        {
            TitleText.Text = $"Reol {number}";
        }
        // Asynkront: hent aktuel orientering uden at blokere UI-tråden
        _ = LoadOrientationAsync(number);
    }

    private async Task LoadOrientationAsync(int number)
    {
        try
        {
            using var scope = App.HostInstance.Services.CreateScope();
            var layout = scope.ServiceProvider.GetRequiredService<IShelfLayoutService>();
            var all = await layout.GetAllAsync();
            var shelf = all.FirstOrDefault(s => s.Number == number);
            if (shelf != null)
            {
                ReolTypeCombo.SelectedIndex = shelf.OrientationHorizontal ? 1 : 0;
            }
        }
        catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
        if (overlay != null)
        {
            overlay.Visibility = Visibility.Collapsed;
        }
    }

    private async void ReolTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_shelfNumber <= 0) return;
        var isHorizontal = ReolTypeCombo.SelectedIndex == 1; // 0=Vertikal, 1=Horizontal
        try
        {
            using var scope = App.HostInstance.Services.CreateScope();
            var layout = scope.ServiceProvider.GetRequiredService<IShelfLayoutService>();
            // Find aktuelle position først
            var all = await layout.GetAllAsync();
            var shelf = all.FirstOrDefault(s => s.Number == _shelfNumber);
            if (shelf == null) return;

            // Opdater DB-orientering – behold samme position
            await layout.TryUpdatePositionAsync(_shelfNumber, shelf.LocationX, shelf.LocationY, isHorizontal);

            // Opdater UI knap-stil nu
            if (System.Windows.Application.Current.MainWindow is MainWindow mw)
            {
                var view = VisualTreeHelpers.FindChildren<ShelfMarket.UI.Views.UserControls.ShelfView>(mw).FirstOrDefault();
                if (view != null)
                {
                    var btn = view.FindName($"Shelf{_shelfNumber}") as Button
                              ?? VisualTreeHelpers.FindChildren<Button>(view).FirstOrDefault(b => (b.Content?.ToString()) == _shelfNumber.ToString());
                    if (btn != null)
                    {
                        var styleKey = isHorizontal ? "Stand.Horizontal" : "Stand.Vertical";
                        if (view.FindResource(styleKey) is Style s)
                        {
                            btn.Style = s;
                        }
                    }
                }
            }
        }
        catch { }
    }
    private void ChooseShelves_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
        if (overlay != null)
        {
            overlay.Visibility = Visibility.Collapsed;
        }
    }

    private async void DeleteShelf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var scope = App.HostInstance.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.ShelfMarketDbContext>();
            var shelf = db.Shelves.FirstOrDefault(s => s.Number == _shelfNumber);
            if (shelf != null)
            {
                db.Shelves.Remove(shelf);
                await db.SaveChangesAsync();
            }

            // Luk overlay
            Close_Click(sender, e);

            // Fjern knappen fra UI med det samme og refresh layout senere
            if (System.Windows.Application.Current.MainWindow is MainWindow mw)
            {
                var view = VisualTreeHelpers.FindChildren<Views.UserControls.ShelfView>(mw).FirstOrDefault();
                if (view != null)
                {
                    Button? btn = view.FindName($"Shelf{_shelfNumber}") as Button;
                    if (btn == null)
                        btn = VisualTreeHelpers.FindChildren<Button>(view).FirstOrDefault(b => (b.Content?.ToString()) == _shelfNumber.ToString());

                    if (btn != null)
                    {
                        if (ReferenceEquals(btn.Parent, view.ShelfGrid))
                        {
                            view.ShelfGrid.Children.Remove(btn);
                        }
                        else if (btn.Parent is Panel panel)
                        {
                            panel.Children.Remove(btn);
                        }
                    }

                    // Trigger reload fra DB for at sikre konsistens
                    var loadMethod = view.GetType().GetMethod("LoadShelvesFromDatabaseAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (loadMethod != null)
                    {
                        _ = (Task?)loadMethod.Invoke(view, null);
                    }
                }
            }
        }
        catch
        {
            // Ignorer fejl for nu
        }
    }
}

// Simple visual tree helpers
static class VisualTreeHelpers
{
    public static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
                return t;
            var result = FindChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    public static IEnumerable<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
                yield return t;
            foreach (var c in FindChildren<T>(child))
                yield return c;
        }
    }
}
