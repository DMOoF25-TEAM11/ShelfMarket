using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using ShelfMarket.UI.Views.Windows;

namespace ShelfMarket.UI.Views
{
    /// <summary>
    /// Brugerflade til visualisering og manuel placering af reoler i et grid.
    /// Understøtter drag-and-drop mellem synlige celler samt popup til oprettelse af nye reoler.
    /// </summary>
    public partial class ShelfView : UserControl
    {
        private bool _isDragging = false;
        private Button? _draggedButton;
        private Point _dragStartPoint;
        private int _originalColumn;
        private int _originalRow;
        private AdornerLayer? _adornerLayer;
        private VisualGhostAdorner? _originGhost;

        public ShelfView()
        {
            InitializeComponent();
            // Vent til UI er indlæst, så alle visuelle elementer findes i visual tree
            this.Loaded += ShelfView_Loaded;
        }

        private void ShelfView_Loaded(object sender, RoutedEventArgs e)
        {
            SetupDragAndDrop();
        }

        /// <summary>
        /// Initialiserer drag-and-drop for alle reolknapper og tilknytter grid-level fallback events.
        /// </summary>
        private void SetupDragAndDrop()
        {
            // Find alle reolknapper i grid'et (søger rekursivt i visual tree)
            var shelfButtons = FindVisualChildren<Button>(ShelfGrid).ToList();
            
            // Debug: Tjek om vi finder knapper
            System.Diagnostics.Debug.WriteLine($"Fundet {shelfButtons.Count} reol knapper");
            
            foreach (var button in shelfButtons)
            {
                button.PreviewMouseLeftButtonDown += Button_MouseLeftButtonDown;
                button.PreviewMouseMove += Button_MouseMove;
                button.PreviewMouseLeftButtonUp += Button_MouseLeftButtonUp;
                                
                button.MouseLeftButtonDown += Button_MouseLeftButtonDown;
                button.MouseMove += Button_MouseMove;
                button.MouseLeftButtonUp += Button_MouseLeftButtonUp;
                button.MouseLeave += Button_MouseLeave;

                button.Cursor = Cursors.Arrow;
                System.Diagnostics.Debug.WriteLine($"Opsat drag for knap: {button.Name}");
            }

            // Tilknyt også mus-events på grid-niveau (handledEventsToo=true sikrer vi fanger boblede events)
            ShelfGrid.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(Grid_MouseMove), true);
            ShelfGrid.AddHandler(UIElement.MouseMoveEvent, new MouseEventHandler(Grid_MouseMove), true);
            ShelfGrid.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(Grid_MouseLeftButtonUp), true);
            ShelfGrid.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(Grid_MouseLeftButtonUp), true);
            
            // Debug: Bekræft at events er tilføjet
            System.Diagnostics.Debug.WriteLine("Drag and drop events er opsat");
        }

        /// <summary>
        /// Starter en drag-operation for den valgte reolknap.
        /// Gemmer udgangspunkt, hæver Z-index, gør knappen semi-transparent og capturer musen.
        /// </summary>
        private void Button_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse down på knap: {button.Name}");
                
                _isDragging = true;
                _draggedButton = button;
                _dragStartPoint = e.GetPosition(ShelfGrid);
                
                // Gem original grid-position til evt. annullering/rollback
                _originalColumn = Grid.GetColumn(button);
                _originalRow = Grid.GetRow(button);
                
                // Opret en origin-ghost adorner ved knappen oprindelige placering
                try
                {
                    _adornerLayer = AdornerLayer.GetAdornerLayer(ShelfGrid);
                    if (_adornerLayer != null)
                    {
                        var originPos = button.TranslatePoint(new Point(0, 0), ShelfGrid);
                        _originGhost = new VisualGhostAdorner(ShelfGrid, button, originPos);
                        _adornerLayer.Add(_originGhost);
                    }
                }
                catch
                {
                    // Ignorer adorner fejl – drag fungerer stadig uden ghost
                }

                // Visuel feedback under drag
                button.Opacity = 0.7;
                Panel.SetZIndex(button, 9999);
                Mouse.OverrideCursor = Cursors.SizeAll;
                
                // Capture musen så vi fortsat modtager events uden for knappen
                button.CaptureMouse();
                
                // Markér event som håndteret for at forhindre yderligere bubbling
                e.Handled = true;
                
                System.Diagnostics.Debug.WriteLine($"Start drag - Original position: Column={_originalColumn}, Row={_originalRow}");
            }
        }

        /// <summary>
        /// Flytter knappen visuelt med musen under drag (RenderTransform).
        /// Selve grid-placeringen opdateres først ved drop.
        /// </summary>
        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedButton != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(ShelfGrid);
                
                // Følg musen med RenderTransform for glat visuel bevægelse
                var delta = currentPosition - _dragStartPoint;
                _draggedButton.RenderTransform = new TranslateTransform(delta.X, delta.Y);
                
                // Markér event som håndteret
                e.Handled = true;
            }
        }

        /// <summary>
        /// Afslutter drag-operationen og snapper til nærmeste synlige grid-celle.
        /// Rydder visuelle effekter og frigiver musen.
        /// </summary>
        private void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedButton != null)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse up - Slut drag operation");
                
                _isDragging = false;
                // Snap til nærmeste synlige celle ved drop
                var releasePosition = e.GetPosition(ShelfGrid);
                var targetColumn = ClampToVisibleColumns(GetColumnFromPosition(releasePosition.X));
                var targetRow = ClampToVisibleRows(GetRowFromPosition(releasePosition.Y));
                // Undgå overlap: hop tilbage hvis målcellen er optaget
                if (IsCellOccupied(targetColumn, targetRow, _draggedButton))
                {
                    Grid.SetColumn(_draggedButton, _originalColumn);
                    Grid.SetRow(_draggedButton, _originalRow);
                }
                else
                {
                    Grid.SetColumn(_draggedButton, targetColumn);
                    Grid.SetRow(_draggedButton, targetRow);
                }

                _draggedButton.Opacity = 1.0;
                _draggedButton.ClearValue(Panel.ZIndexProperty);
                _draggedButton.RenderTransform = Transform.Identity;
                _draggedButton.ReleaseMouseCapture();
                _draggedButton = null;
                Mouse.OverrideCursor = null;
                RemoveGhost();
                
                // Markér event som håndteret
                e.Handled = true;
            }
        }

        /// <summary>
        /// Kaldes når musen forlader knappen under drag.
        /// Drag fortsætter, da grid-level events håndterer bevægelser uden for knappen.
        /// </summary>
        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            // Intet at gøre her; se Grid_MouseMove
        }

        /// <summary>
        /// Håndterer mus-bevægelse på grid-niveau under drag som fallback,
        /// så vi kan fortsætte drag selvom markøren ikke længere er over knappen.
        /// </summary>
        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedButton != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(ShelfGrid);
                
                // Følg musen med RenderTransform
                var delta = currentPosition - _dragStartPoint;
                _draggedButton.RenderTransform = new TranslateTransform(delta.X, delta.Y);
                
                // Markér event som håndteret
                e.Handled = true;
            }
        }

        /// <summary>
        /// Afslutter drag på grid-niveau (hvis knappen ikke selv modtager mouse up).
        /// Snapper til nærmeste synlige celle og rydder visuelle effekter.
        /// </summary>
        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedButton != null)
            {
                System.Diagnostics.Debug.WriteLine($"Grid Mouse up - Slut drag operation");
                
                _isDragging = false;
                // Snap til nærmeste synlige celle
                var releasePosition = e.GetPosition(ShelfGrid);
                var targetColumn = ClampToVisibleColumns(GetColumnFromPosition(releasePosition.X));
                var targetRow = ClampToVisibleRows(GetRowFromPosition(releasePosition.Y));
                // Undgå overlap: hop tilbage hvis målcellen er optaget
                if (IsCellOccupied(targetColumn, targetRow, _draggedButton))
                {
                    Grid.SetColumn(_draggedButton, _originalColumn);
                    Grid.SetRow(_draggedButton, _originalRow);
                }
                else
                {
                    Grid.SetColumn(_draggedButton, targetColumn);
                    Grid.SetRow(_draggedButton, targetRow);
                }

                _draggedButton.Opacity = 1.0;
                _draggedButton.RenderTransform = Transform.Identity;
                _draggedButton.ReleaseMouseCapture();
                _draggedButton = null;
                Mouse.OverrideCursor = null;
                RemoveGhost();
                
                // Markér event som håndteret
                e.Handled = true;
            }
        }

        private void RemoveGhost()
        {
            try
            {
                if (_originGhost != null && _adornerLayer != null)
                {
                    _adornerLayer.Remove(_originGhost);
                }
            }
            finally
            {
                _originGhost = null;
                _adornerLayer = null;
            }
        }

        /// <summary>
        /// Simpel adorner der tegner en semitransparent kopi af et UIElement på en fast position.
        /// </summary>
        private sealed class VisualGhostAdorner : Adorner
        {
            private readonly VisualBrush _brush;
            private readonly double _width;
            private readonly double _height;
            private readonly Point _location;

            public VisualGhostAdorner(UIElement adornedElement, UIElement ghostOf, Point location)
                : base(adornedElement)
            {
                _width = (ghostOf as FrameworkElement)?.ActualWidth ?? 0;
                _height = (ghostOf as FrameworkElement)?.ActualHeight ?? 0;
                _brush = new VisualBrush(ghostOf)
                {
                    Opacity = 0.35,
                    Stretch = Stretch.None
                };
                _location = location;
                IsHitTestVisible = false;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);
                drawingContext.DrawRectangle(_brush, null, new Rect(_location, new Size(_width, _height)));
            }
        }

        /// <summary>
        /// Udregner kolonneindeks ud fra X-position i forhold til akkumulerede kolonnebredder.
        /// Ignorerer skjulte (0-bredde) kolonner.
        /// </summary>
        private int GetColumnFromPosition(double x)
        {
            // Brug faktiske bredder; fald tilbage til deklareret absolut bredde hvis nødvendig
            double accumulatedWidth = 0;
            for (int i = 0; i < ShelfGrid.ColumnDefinitions.Count; i++)
            {
                var col = ShelfGrid.ColumnDefinitions[i];
                double width = col.ActualWidth;
                if (double.IsNaN(width) || width <= 0)
                {
                    width = col.Width.IsAbsolute ? col.Width.Value : 0;
                }

                accumulatedWidth += width;
                if (x < accumulatedWidth)
                {
                    return i;
                }
            }

            return ShelfGrid.ColumnDefinitions.Count - 1;
        }

        /// <summary>
        /// Udregner rækkeindeks ud fra Y-position i forhold til akkumulerede rækkehøjder.
        /// Ignorerer skjulte (0-højde) rækker.
        /// </summary>
        private int GetRowFromPosition(double y)
        {
            double accumulatedHeight = 0;
            for (int i = 0; i < ShelfGrid.RowDefinitions.Count; i++)
            {
                var row = ShelfGrid.RowDefinitions[i];
                double height = row.ActualHeight;
                if (double.IsNaN(height) || height <= 0)
                {
                    height = row.Height.IsAbsolute ? row.Height.Value : 0;
                }

                accumulatedHeight += height;
                if (y < accumulatedHeight)
                {
                    return i;
                }
            }

            return ShelfGrid.RowDefinitions.Count - 1;
        }

        /// <summary>
        /// Tjekker om en grid-position ligger inden for første og sidste synlige række/kolonne.
        /// </summary>
        private bool IsValidGridPosition(int column, int row)
        {
            // Begræns til synlige celler (spring 0-størrelse rækker/kolonner over)
            int firstVisibleCol = 0;
            while (firstVisibleCol < ShelfGrid.ColumnDefinitions.Count &&
                   GetDefinitionSize(ShelfGrid.ColumnDefinitions[firstVisibleCol]) <= 0)
            {
                firstVisibleCol++;
            }

            int lastVisibleCol = ShelfGrid.ColumnDefinitions.Count - 1;
            while (lastVisibleCol >= 0 &&
                   GetDefinitionSize(ShelfGrid.ColumnDefinitions[lastVisibleCol]) <= 0)
            {
                lastVisibleCol--;
            }

            int firstVisibleRow = 0;
            while (firstVisibleRow < ShelfGrid.RowDefinitions.Count &&
                   GetDefinitionSize(ShelfGrid.RowDefinitions[firstVisibleRow]) <= 0)
            {
                firstVisibleRow++;
            }

            int lastVisibleRow = ShelfGrid.RowDefinitions.Count - 1;
            while (lastVisibleRow >= 0 &&
                   GetDefinitionSize(ShelfGrid.RowDefinitions[lastVisibleRow]) <= 0)
            {
                lastVisibleRow--;
            }

            return column >= firstVisibleCol && column <= lastVisibleCol &&
                   row >= firstVisibleRow && row <= lastVisibleRow;
        }

        private int ClampToVisibleColumns(int column)
        {
            int firstVisibleCol = 0;
            while (firstVisibleCol < ShelfGrid.ColumnDefinitions.Count &&
                   GetDefinitionSize(ShelfGrid.ColumnDefinitions[firstVisibleCol]) <= 0)
            {
                firstVisibleCol++;
            }

            int lastVisibleCol = ShelfGrid.ColumnDefinitions.Count - 1;
            while (lastVisibleCol >= 0 &&
                   GetDefinitionSize(ShelfGrid.ColumnDefinitions[lastVisibleCol]) <= 0)
            {
                lastVisibleCol--;
            }

            return Math.Max(firstVisibleCol, Math.Min(lastVisibleCol, column));
        }

        private int ClampToVisibleRows(int row)
        {
            int firstVisibleRow = 0;
            while (firstVisibleRow < ShelfGrid.RowDefinitions.Count &&
                   GetDefinitionSize(ShelfGrid.RowDefinitions[firstVisibleRow]) <= 0)
            {
                firstVisibleRow++;
            }

            int lastVisibleRow = ShelfGrid.RowDefinitions.Count - 1;
            while (lastVisibleRow >= 0 &&
                   GetDefinitionSize(ShelfGrid.RowDefinitions[lastVisibleRow]) <= 0)
            {
                lastVisibleRow--;
            }

            return Math.Max(firstVisibleRow, Math.Min(lastVisibleRow, row));
        }

        private static double GetDefinitionSize(ColumnDefinition col)
        {
            var width = col.ActualWidth;
            if (double.IsNaN(width) || width <= 0)
            {
                width = col.Width.IsAbsolute ? col.Width.Value : 0;
            }
            return width;
        }

        private static double GetDefinitionSize(RowDefinition row)
        {
            var height = row.ActualHeight;
            if (double.IsNaN(height) || height <= 0)
            {
                height = row.Height.IsAbsolute ? row.Height.Value : 0;
            }
            return height;
        }

        private double GetCellWidth()
        {
            // Estimér typisk cellebredde via første synlige kolonne; brug fallback hvis intet findes
            foreach (var col in ShelfGrid.ColumnDefinitions)
            {
                var w = col.ActualWidth;
                if (w > 0) return w;
                if (col.Width.IsAbsolute && col.Width.Value > 0) return col.Width.Value;
            }
            return 51.0;
        }

        private double GetCellHeight()
        {
            // Estimér typisk cellehøjde via første synlige række; brug fallback hvis intet findes
            foreach (var row in ShelfGrid.RowDefinitions)
            {
                var h = row.ActualHeight;
                if (h > 0) return h;
                if (row.Height.IsAbsolute && row.Height.Value > 0) return row.Height.Value;
            }
            return 51.0;
        }

        /// <summary>
        /// Hjælpemetode der finder alle visuelle børn af typen T (DFS gennem visual tree).
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null)
                    {
                        if (child is T)
                        {
                            yield return (T)child;
                        }
                        foreach (T childOfChild in FindVisualChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Åbner overlay/popup til oprettelse af en ny reol og tilknytter nødvendige events.
        /// </summary>
        private void TilfoejNyReol_Click(object sender, RoutedEventArgs e)
        {
            // Find MainWindow og relaterede overlay-komponenter
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var overlay = mainWindow.FindName("PopupOverlay") as Grid;
                var popupContent = mainWindow.FindName("AddShelfPopupContent") as AddShelfWindow;
                
                if (overlay != null && popupContent != null)
                {
                    // Tilknyt events (frakobles igen ved lukning for at undgå memory leaks)
                    popupContent.ReolTilfoejet += OnReolTilfoejet;
                    popupContent.Annulleret += OnPopupAnnulleret;
                    
                    // Nulstil popup til standardværdier
                    popupContent.Nulstil();
                    
                    // Vis overlay
                    overlay.Visibility = Visibility.Visible;
                    popupContent.Visibility = Visibility.Visible;
                    var addContract = mainWindow.FindName("AddContractPopupContent") as AddContractWindow;
                    if (addContract != null) addContract.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void NyKontrakt_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var overlay = mainWindow.FindName("PopupOverlay") as Grid;
                var addContract = mainWindow.FindName("AddContractPopupContent") as AddContractWindow;
                if (overlay != null && addContract != null)
                {
                    overlay.Visibility = Visibility.Visible;
                    addContract.Visibility = Visibility.Visible;
                    var addShelf = mainWindow.FindName("AddShelfPopupContent") as AddShelfWindow;
                    if (addShelf != null) addShelf.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Kaldes når en ny reol er tilføjet via popup.
        /// </summary>
        private void OnReolTilfoejet(object? sender, EventArgs e)
        {
            MessageBox.Show("Reol er tilføjet succesfuldt!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Luk overlay
            LukOverlay();
        }

        /// <summary>
        /// Lukker overlay hvis brugeren annullerer oprettelsen af en reol.
        /// </summary>
        private void OnPopupAnnulleret(object? sender, EventArgs e)
        {
            // Luk overlay uden at tilføje reol
            LukOverlay();
        }

        /// <summary>
        /// Lukker overlay og frakobler popup-events for at forhindre memory leaks.
        /// </summary>
        private void LukOverlay()
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
            var popupContent = mainWindow?.FindName("AddShelfPopupContent") as AddShelfWindow;
            var addContract = mainWindow?.FindName("AddContractPopupContent") as AddContractWindow;
            
            if (overlay != null)
            {
                overlay.Visibility = Visibility.Collapsed;
            }
            
            // Frakobl events for at undgå memory leaks
            if (popupContent != null)
            {
                popupContent.ReolTilfoejet -= OnReolTilfoejet;
                popupContent.Annulleret -= OnPopupAnnulleret;
            }

            // Skjul også AddContract hvis den var åben
            if (addContract != null)
            {
                addContract.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Returnerer true hvis der allerede står en anden reolknap i den angivne celle.
        /// </summary>
        private bool IsCellOccupied(int column, int row, Button? ignore = null)
        {
            foreach (var child in FindVisualChildren<Button>(ShelfGrid))
            {
                if (ignore != null && ReferenceEquals(child, ignore)) continue;
                if (Grid.GetColumn(child) == column && Grid.GetRow(child) == row)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
