using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Interfaces;
using ShelfMarket.UI.Views.Windows;

namespace ShelfMarket.UI.Views
{
    /// <summary>
    /// Brugerflade til visualisering og manuel placering af reoler i et grid.
    /// Understøtter drag-and-drop mellem synlige celler samt popup til oprettelse af nye reoler.
    /// </summary>
    public partial class ShelfView : UserControl
    {
        #region variables
        // Grid shape (19 x 27), cell size in pixels
        private const int GridColumns = 19;
        private const int GridRows = 28;
        private const double CellSizePx = 25.0;
        #endregion

        #region Fields
        private bool _isDragging = false;
        private Button? _draggedButton;
        private Point _dragStartPoint;
        private int _originalColumn;
        private int _originalRow;
        private AdornerLayer? _adornerLayer;
        private VisualGhostAdorner? _originGhost;
        private bool _potentialClick;
        private DateTime _clickStart;
        private Button? _clickedButton;
        #endregion

        #region Initialization
        public ShelfView()
        {
            InitializeComponent();
            // Vent til UI er indlæst, så alle visuelle elementer findes i visual tree
            this.Loaded += ShelfView_Loaded;
        }

        /// <summary>
        /// View is ready: wire up drag & drop and render shelves from database.
        /// The database is the single source of truth for locations/orientation.
        /// </summary>
        private async void ShelfView_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigureShelfGridShape();
            SetupDragAndDrop();
            await LoadShelvesFromDatabaseAsync();
        }
        #endregion

        #region Data load
        /// <summary>
        /// Loads shelves from persistence and applies Grid position and style
        /// (horizontal/vertical) to the matching WPF buttons (Shelf1..Shelf80).
        /// </summary>
        private async Task LoadShelvesFromDatabaseAsync()
        {
            try
            {
                using var scope = App.HostInstance.Services.CreateScope();
                var layoutService = scope.ServiceProvider.GetRequiredService<IShelfLayoutService>();
                var shelves = await layoutService.GetAllAsync();

                foreach (var shelf in shelves)
                {
                    var name = $"Shelf{shelf.Number}";
                    var button = FindName(name) as Button ?? FindButtonByName(ShelfGrid, name);

                    if (button == null)
                    {
                        // Create button dynamically for shelves not present in XAML (e.g., Shelf81+)
                        button = new Button
                        {
                            Name = name,
                            Content = shelf.Number.ToString(),
                            ToolTip = $"Reol {shelf.Number}",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            Cursor = Cursors.Arrow
                        };

                        // Attach drag/drop events so it behaves like existing buttons
                        button.PreviewMouseLeftButtonDown += Button_MouseLeftButtonDown;
                        button.PreviewMouseMove += Button_MouseMove;
                        button.PreviewMouseLeftButtonUp += Button_MouseLeftButtonUp;
                        button.MouseLeftButtonDown += Button_MouseLeftButtonDown;
                        button.MouseMove += Button_MouseMove;
                        button.MouseLeftButtonUp += Button_MouseLeftButtonUp;
                        button.MouseLeave += Button_MouseLeave;

                        ShelfGrid.Children.Add(button);
                    }

                    // Apply style based on orientation
                    try
                    {
                        var styleKey = shelf.OrientationHorizontal ? "Stand.Horizontal" : "Stand.Vertical";
                        if (FindResource(styleKey) is Style s)
                        {
                            button.Style = s;
                        }
                    }
                    catch { }

                    // Apply span: horizontal occupies 2 columns, vertical occupies 2 rows
                    ApplyShelfSpan(button, shelf.OrientationHorizontal);

                    // Clamp to visible grid range considering span
                    var spanX = Grid.GetColumnSpan(button) is int cx && cx > 0 ? cx : 1;
                    var spanY = Grid.GetRowSpan(button) is int cy && cy > 0 ? cy : 1;

                    var col = ClampToVisibleColumnsForSpan(Math.Max(0, shelf.LocationX), spanX);
                    var row = ClampToVisibleRowsForSpan(Math.Max(0, shelf.LocationY), spanY);

                    Grid.SetColumn(button, col);
                    Grid.SetRow(button, row);
                }
            }
            catch
            {
                // Silent: layout will remain default if DB is unavailable
            }
        }
        #endregion

        #region Drag & drop wiring
        //private async Task EnsureMissingShelvesCreatedFromUIAsync(IEnumerable<ShelfMarket.Domain.Entities.Shelf> existing)
        //{
        //    try
        //    {
        //        var existingByNumber = existing.ToDictionary(s => s.Number, s => s);
        //        var allButtons = FindVisualChildren<Button>(ShelfGrid).Where(b => b.Name.StartsWith("Shelf")).ToList();

        //        // Resolve style references once
        //        Style? styleH = null, styleV = null;
        //        try
        //        {
        //            styleH = FindResource("Stand.Horizontal") as Style;
        //            styleV = FindResource("Stand.Vertical") as Style;
        //        }
        //        catch { }

        //        var missing = new List<(int Number, int Col, int Row, bool IsHorizontal)>();
        //        var updates = new List<(ShelfMarket.Domain.Entities.Shelf Shelf, int Col, int Row, bool? NewHorizontal)>();

        //        foreach (var b in allButtons)
        //        {
        //            if (!int.TryParse(b.Name["Shelf".Length..], out var num)) continue;
        //            int col = Grid.GetColumn(b);
        //            int row = Grid.GetRow(b);
        //            bool isHorizontal = Grid.GetColumnSpan(b) > 1; // use span, falls back to style
        //            if (!isHorizontal && styleH != null && ReferenceEquals(b.Style, styleH)) isHorizontal = true;

        //            /*
        //             * TODO : Use IsLocationFreeAsync from ShelfRepository to check if position is valid
        //             */
        //            if (existingByNumber.TryGetValue(num, out var shelf))
        //            {
        //                bool needUpdatePos = shelf.LocationX != col || shelf.LocationY != row;
        //                bool needUpdateOri = shelf.OrientationHorizontal != isHorizontal;
        //                if (needUpdatePos || needUpdateOri)
        //                    updates.Add((shelf, col, row, needUpdateOri ? isHorizontal : (bool?)null));
        //            }
        //            else
        //            {
        //                missing.Add((num, col, row, isHorizontal));
        //            }
        //        }

        //        // DB er nu source of truth – ingen automatisk reset længere
        //        return;

        //        // (seeding code below kept as-is but not executed)
        //    }
        //    catch
        //    {
        //        // ignore – seeding er kun convenience
        //    }
        //}

        //private static string shellfNumber(uint number) => number.ToString();

        private static Button? FindButtonByName(DependencyObject root, string name)
        {
            foreach (var b in FindVisualChildren<Button>(root))
            {
                if (b.Name == name) return b;
            }
            return null;
        }
        #endregion

        #region Drag & drop handlers
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

                // Forbered klik/drag, men start ikke drag endnu
                _potentialClick = true;
                _clickStart = DateTime.Now;
                _clickedButton = button;
                _draggedButton = button;
                _dragStartPoint = e.GetPosition(ShelfGrid);
                _originalColumn = Grid.GetColumn(button);
                _originalRow = Grid.GetRow(button);
                // Vent med at capture musen og visuals til vi har passeret threshold i MouseMove
                e.Handled = true;
            }
        }

        /// <summary>
        /// Flytter knappen visuelt med musen under drag (RenderTransform).
        /// Selve grid-placeringen opdateres først ved drop.
        /// </summary>
        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var current = e.GetPosition(ShelfGrid);
                // Start drag når vi passerer threshold
                if (!_isDragging && _draggedButton != null &&
                    (Math.Abs(current.X - _dragStartPoint.X) > 5 || Math.Abs(current.Y - _dragStartPoint.Y) > 5))
                {
                    _isDragging = true;
                    _potentialClick = false;

                    // Opret ghost-adorner ved knappen oprindelige placering
                    try
                    {
                        _adornerLayer = AdornerLayer.GetAdornerLayer(ShelfGrid);
                        if (_adornerLayer != null && _draggedButton != null)
                        {
                            var originPos = _draggedButton.TranslatePoint(new Point(0, 0), ShelfGrid);
                            _originGhost = new VisualGhostAdorner(ShelfGrid, _draggedButton, originPos);
                            _adornerLayer.Add(_originGhost);
                        }
                    }
                    catch { _adornerLayer = null; _originGhost = null; }

                    _draggedButton.Opacity = 0.7;
                    Panel.SetZIndex(_draggedButton, 9999);
                    Mouse.OverrideCursor = Cursors.SizeAll;
                    _draggedButton.CaptureMouse();
                    System.Diagnostics.Debug.WriteLine($"Start drag - Original position: Column={_originalColumn}, Row={_originalRow}");
                }

                if (_isDragging && _draggedButton != null)
                {
                    var delta = current - _dragStartPoint;
                    _draggedButton.RenderTransform = new TranslateTransform(delta.X, delta.Y);
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// Afslutter drag-operationen og snapper til nærmeste synlige grid-celle.
        /// Rydder visuelle effekter og frigiver musen.
        /// </summary>
        private async void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedButton != null)
            {
                if (!_isDragging && _potentialClick && _clickedButton == _draggedButton && (DateTime.Now - _clickStart).TotalMilliseconds < 250)
                {
                    // Hurtigt klik: åbn info-vindue
                    OpenShelfInfoForButton(_draggedButton);
                    _draggedButton.RenderTransform = null;
                    _draggedButton.Opacity = 1.0;
                    Panel.SetZIndex(_draggedButton, 0);
                    _draggedButton.ReleaseMouseCapture();
                    Mouse.OverrideCursor = null;
                    e.Handled = true;
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"Mouse up - Slut drag operation");
                _isDragging = false;

                var releasePosition = e.GetPosition(ShelfGrid);

                // Clamp considering span (horizontal = 2x1, vertical = 1x2)
                var button = _draggedButton; // snapshot
                var spanX = Math.Max(1, Grid.GetColumnSpan(button));
                var spanY = Math.Max(1, Grid.GetRowSpan(button));

                var targetColumn = ClampToVisibleColumnsForSpan(GetColumnFromPosition(releasePosition.X), spanX);
                var targetRow = ClampToVisibleRowsForSpan(GetRowFromPosition(releasePosition.Y), spanY);

                // Persistér først; hvis gem fejler, ruller vi tilbage
                if (!await TryPersistMoveAsync(button, targetColumn, targetRow, _originalColumn, _originalRow))
                {
                    Grid.SetColumn(button, _originalColumn);
                    Grid.SetRow(button, _originalRow);
                }
                else
                {
                    Grid.SetColumn(button, targetColumn);
                    Grid.SetRow(button, targetRow);
                }

                button.Opacity = 1.0;
                button.ClearValue(Panel.ZIndexProperty);
                button.RenderTransform = Transform.Identity;
                button.ReleaseMouseCapture();
                RemoveGhost();
                _draggedButton = null;
                Mouse.OverrideCursor = null;
                RemoveGhost();

                e.Handled = true;
            }
        }
        #endregion

        #region Grid-level fallbacks
        private void OpenShelfInfoForButton(Button button)
        {
            if (!int.TryParse(button.Content?.ToString(), out var number)) return;
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var overlay = mainWindow?.FindName("PopupOverlay") as Grid;
            var info = mainWindow?.FindName("ShelfInfoPopupContent") as ShelfMarket.UI.Views.Windows.ShelfInfoWindow;
            if (overlay != null && info != null)
            {
                info.SetShelfNumber(number);
                overlay.Visibility = Visibility.Visible;
                info.Visibility = Visibility.Visible;
            }
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            // Intet at gøre her; se Grid_MouseMove
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedButton != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(ShelfGrid);

                // Følg musen med RenderTransform
                var delta = currentPosition - _dragStartPoint;
                _draggedButton.RenderTransform = new TranslateTransform(delta.X, delta.Y);

                e.Handled = true;
            }
        }

        private async void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && _draggedButton != null)
            {
                System.Diagnostics.Debug.WriteLine($"Grid Mouse up - Slut drag operation");

                _isDragging = false;

                var releasePosition = e.GetPosition(ShelfGrid);

                // Clamp considering span (horizontal = 2x1, vertical = 1x2)
                var button = _draggedButton; // snapshot
                var spanX = Math.Max(1, Grid.GetColumnSpan(button));
                var spanY = Math.Max(1, Grid.GetRowSpan(button));

                var targetColumn = ClampToVisibleColumnsForSpan(GetColumnFromPosition(releasePosition.X), spanX);
                var targetRow = ClampToVisibleRowsForSpan(GetRowFromPosition(releasePosition.Y), spanY);

                // Persistér først; hvis gem fejler, ruller vi tilbage
                if (!await TryPersistMoveAsync(button, targetColumn, targetRow, _originalColumn, _originalRow))
                {
                    Grid.SetColumn(button, _originalColumn);
                    Grid.SetRow(button, _originalRow);
                }
                else
                {
                    Grid.SetColumn(button, targetColumn);
                    Grid.SetRow(button, targetRow);
                }

                button.Opacity = 1.0;
                button.RenderTransform = Transform.Identity;
                button.ReleaseMouseCapture();
                RemoveGhost();
                _draggedButton = null;
                Mouse.OverrideCursor = null;
                RemoveGhost();

                e.Handled = true;
            }
        }
        #endregion

        #region Persistence
        private async Task<bool> TryPersistMoveAsync(Button button, int targetColumn, int targetRow, int rollbackColumn, int rollbackRow)
        {
            try
            {
                var numberStr = button.Name.StartsWith("Shelf") ? button.Name["Shelf".Length..] : string.Empty;
                if (!int.TryParse(numberStr, out var number)) return false;

                bool isHorizontal = false;
                try
                {
                    // Prefer span to infer orientation
                    isHorizontal = Grid.GetColumnSpan(button) > 1;
                    if (!isHorizontal)
                    {
                        var h = FindResource("Stand.Horizontal") as Style;
                        if (h != null && ReferenceEquals(button.Style, h)) isHorizontal = true;
                        else
                        {
                            var v = FindResource("Stand.Vertical") as Style;
                            if (v != null && ReferenceEquals(button.Style, v) == false) isHorizontal = true; // fallback guess
                        }
                    }
                }
                catch { }

                using var scope = App.HostInstance.Services.CreateScope();
                var layoutService = scope.ServiceProvider.GetRequiredService<IShelfLayoutService>();
                var ok = await layoutService.TryUpdatePositionAsync(number, targetColumn, targetRow, isHorizontal);
                if (!ok)
                {
                    try
                    {
                        var all = await layoutService.GetAllAsync();
                        var shelf = all.FirstOrDefault(s => s.Number == number);
                        bool shelfExists = shelf != null;
                        bool occupied = all.Any(s => s.LocationX == targetColumn && s.LocationY == targetRow && s.Number != number);
                        if (!shelfExists)
                        {
                            MessageBox.Show($"Reol {number} findes ikke i databasen endnu. Jeg opretter den nu ved første flyt.", "Manglende reol", MessageBoxButton.OK, MessageBoxImage.Information);
                            try
                            {
                                using var scopeCreate = App.HostInstance.Services.CreateScope();
                                var db = scopeCreate.ServiceProvider.GetRequiredService<ShelfMarket.Infrastructure.Persistence.ShelfMarketDbContext>();
                                var type = db.ShelfTypes.FirstOrDefault();
                                var typeId = type?.Id ?? Guid.NewGuid();
                                if (type == null)
                                {
                                    db.ShelfTypes.Add(new ShelfMarket.Domain.Entities.ShelfType { Id = typeId, Name = "Default" });
                                }
                                db.Shelves.Add(new ShelfMarket.Domain.Entities.Shelf
                                {
                                    Id = Guid.NewGuid(),
                                    Number = number,
                                    ShelfTypeId = typeId,
                                    LocationX = targetColumn,
                                    LocationY = targetRow,
                                    OrientationHorizontal = isHorizontal
                                });
                                await db.SaveChangesAsync();
                                return true;
                            }
                            catch
                            {
                            }
                        }
                        else if (occupied)
                        {
                            // Occupied target: silent rollback
                        }
                    }
                    catch { }
                }
                return ok;
            }
            catch (Exception ex)
            {
                try { MessageBox.Show($"Kunne ikke gemme reolens position:\n{ex.Message}", "Fejl ved gem", MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
                return false;
            }
        }
        #endregion

        #region Adorner/ghost helpers
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
        #endregion

        #region Grid helpers
        private int GetColumnFromPosition(double x)
        {
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

        //[Obsolete("Not used anymore. using repository methods instead")]
        //private bool IsValidGridPosition(int column, int row)
        //{
        //    int firstVisibleCol = 0;
        //    while (firstVisibleCol < ShelfGrid.ColumnDefinitions.Count &&
        //           GetDefinitionSize(ShelfGrid.ColumnDefinitions[firstVisibleCol]) <= 0)
        //    {
        //        firstVisibleCol++;
        //    }

        //    int lastVisibleCol = ShelfGrid.ColumnDefinitions.Count - 1;
        //    while (lastVisibleCol >= 0 &&
        //           GetDefinitionSize(ShelfGrid.ColumnDefinitions[lastVisibleCol]) <= 0)
        //    {
        //        lastVisibleCol--;
        //    }

        //    int firstVisibleRow = 0;
        //    while (firstVisibleRow < ShelfGrid.RowDefinitions.Count &&
        //           GetDefinitionSize(ShelfGrid.RowDefinitions[firstVisibleRow]) <= 0)
        //    {
        //        firstVisibleRow++;
        //    }

        //    int lastVisibleRow = ShelfGrid.RowDefinitions.Count - 1;
        //    while (lastVisibleRow >= 0 &&
        //           GetDefinitionSize(ShelfGrid.RowDefinitions[lastVisibleRow]) <= 0)
        //    {
        //        lastVisibleRow--;
        //    }

        //    return column >= firstVisibleCol && column <= lastVisibleCol &&
        //           row >= firstVisibleRow && row <= lastVisibleRow;
        //}

        //private int ClampToVisibleColumns(int column)
        //{
        //    int firstVisibleCol = 0;
        //    while (firstVisibleCol < ShelfGrid.ColumnDefinitions.Count &&
        //           GetDefinitionSize(ShelfGrid.ColumnDefinitions[firstVisibleCol]) <= 0)
        //    {
        //        firstVisibleCol++;
        //    }

        //    int lastVisibleCol = ShelfGrid.ColumnDefinitions.Count - 1;
        //    while (lastVisibleCol >= 0 &&
        //           GetDefinitionSize(ShelfGrid.ColumnDefinitions[lastVisibleCol]) <= 0)
        //    {
        //        lastVisibleCol--;
        //    }

        //    return Math.Max(firstVisibleCol, Math.Min(lastVisibleCol, column));
        //}

        //private int ClampToVisibleRows(int row)
        //{
        //    int firstVisibleRow = 0;
        //    while (firstVisibleRow < ShelfGrid.RowDefinitions.Count &&
        //           GetDefinitionSize(ShelfGrid.RowDefinitions[firstVisibleRow]) <= 0)
        //    {
        //        firstVisibleRow++;
        //    }

        //    int lastVisibleRow = ShelfGrid.RowDefinitions.Count - 1;
        //    while (lastVisibleRow >= 0 &&
        //           GetDefinitionSize(ShelfGrid.RowDefinitions[lastVisibleRow]) <= 0)
        //    {
        //        lastVisibleRow--;
        //    }

        //    return Math.Max(firstVisibleRow, Math.Min(lastVisibleRow, row));
        //}

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

        //private double GetCellWidth()
        //{
        //    // Estimér typisk cellebredde via første synlige kolonne; brug fallback hvis intet findes
        //    foreach (var col in ShelfGrid.ColumnDefinitions)
        //    {
        //        var w = col.ActualWidth;
        //        if (w > 0) return w;
        //        if (col.Width.IsAbsolute && col.Width.Value > 0) return col.Width.Value;
        //    }
        //    return 25.0;
        //}

        //private double GetCellHeight()
        //{
        //    // Estimér typisk cellehøjde via første synlige række; brug fallback hvis intet findes
        //    foreach (var row in ShelfGrid.RowDefinitions)
        //    {
        //        var h = row.ActualHeight;
        //        if (h > 0) return h;
        //        if (row.Height.IsAbsolute && row.Height.Value > 0) return row.Height.Value;
        //    }
        //    return 25.0;
        //}
        #endregion

        #region Shelf sizing helpers
        /// <summary>
        /// Sets the Grid spans so a shelf occupies two cells horizontally (2x1) or vertically (1x2).
        /// </summary>
        private static void ApplyShelfSpan(Button button, bool isHorizontal)
        {
            if (isHorizontal)
            {
                Grid.SetColumnSpan(button, 2);
                Grid.SetRowSpan(button, 1);
            }
            else
            {
                Grid.SetColumnSpan(button, 1);
                Grid.SetRowSpan(button, 2);
            }
        }

        /// <summary>
        /// Clamp top-left column so [column .. column+spanX-1] stays inside last visible column.
        /// </summary>
        private int ClampToVisibleColumnsForSpan(int column, int spanX)
        {
            spanX = Math.Max(spanX, 1);
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

            int maxStart = Math.Max(firstVisibleCol, lastVisibleCol - (spanX - 1));
            return Math.Max(firstVisibleCol, Math.Min(maxStart, column));
        }

        /// <summary>
        /// Clamp top-left row so [row .. row+spanY-1] stays inside last visible row.
        /// </summary>
        private int ClampToVisibleRowsForSpan(int row, int spanY)
        {
            spanY = Math.Max(spanY, 1);
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

            int maxStart = Math.Max(firstVisibleRow, lastVisibleRow - (spanY - 1));
            return Math.Max(firstVisibleRow, Math.Min(maxStart, row));
        }
        #endregion

        #region Visual tree + overlay/popups
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

        private void TilfoejNyReol_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var overlay = mainWindow.FindName("PopupOverlay") as Grid;
                var popupContent = mainWindow.FindName("AddShelfPopupContent") as AddShelfWindow;

                if (overlay != null && popupContent != null)
                {
                    popupContent.ReolTilfoejet += OnReolTilfoejet;
                    popupContent.Annulleret += OnPopupAnnulleret;

                    popupContent.Nulstil();

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

        private void OnReolTilfoejet(object? sender, EventArgs e)
        {
            _ = LoadShelvesFromDatabaseAsync();
            LukOverlay();
        }

        private void OnPopupAnnulleret(object? sender, EventArgs e)
        {
            LukOverlay();
        }

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

            if (popupContent != null)
            {
                popupContent.ReolTilfoejet -= OnReolTilfoejet;
                popupContent.Annulleret -= OnPopupAnnulleret;
            }

            if (addContract != null)
            {
                addContract.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Returnerer true hvis der allerede står en anden reolknap i den angivne celle.
        /// Note: Kept for compatibility; occupancy is validated server-side.
        /// </summary>
        //private bool IsCellOccupied(int column, int row, Button? ignore = null)
        //{
        //    foreach (var child in FindVisualChildren<Button>(ShelfGrid))
        //    {
        //        if (ignore != null && ReferenceEquals(child, ignore)) continue;
        //        if (Grid.GetColumn(child) == column && Grid.GetRow(child) == row)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        #endregion

        #region Grid configuration
        /// <summary>
        /// Enforces a 19x27 grid with 25px cells and sizes the grid to match those definitions.
        /// Prevents stretching by aligning to top-left.
        /// </summary>
        private void ConfigureShelfGridShape()
        {
            if (ShelfGrid == null) return;

            // Stop layout pass while reconfiguring to avoid flicker
            ShelfGrid.BeginInit();

            // Exact size = columns * cell width, rows * cell height
            ShelfGrid.HorizontalAlignment = HorizontalAlignment.Left;
            ShelfGrid.VerticalAlignment = VerticalAlignment.Top;
            ShelfGrid.Width = GridColumns * CellSizePx;
            ShelfGrid.Height = GridRows * CellSizePx;

            // Ensure column definitions
            if (ShelfGrid.ColumnDefinitions.Count != GridColumns ||
                ShelfGrid.ColumnDefinitions.Any(c => !c.Width.IsAbsolute || Math.Abs(c.Width.Value - CellSizePx) > 0.01))
            {
                ShelfGrid.ColumnDefinitions.Clear();
                for (int i = 0; i < GridColumns; i++)
                {
                    ShelfGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(CellSizePx) });
                }
            }

            // Ensure row definitions
            if (ShelfGrid.RowDefinitions.Count != GridRows ||
                ShelfGrid.RowDefinitions.Any(r => !r.Height.IsAbsolute || Math.Abs(r.Height.Value - CellSizePx) > 0.01))
            {
                ShelfGrid.RowDefinitions.Clear();
                for (int i = 0; i < GridRows; i++)
                {
                    ShelfGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(CellSizePx) });
                }
            }

            ShelfGrid.EndInit();
        }
        #endregion

    }
}
