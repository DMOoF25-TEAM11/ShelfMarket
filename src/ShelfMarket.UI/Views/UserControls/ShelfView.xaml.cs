using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstract.Services;
using ShelfMarket.UI.Views.Windows;

namespace ShelfMarket.UI.Views.UserControls;

/// <summary>
/// UI for visualizing and manually placing shelves in a fixed-size grid.
/// Supports drag-and-drop between visible cells and popups for creating and inspecting shelves.
/// The database is the single source of truth for locations/orientation.
/// </summary>
public partial class ShelfView : UserControl
{
    /*
     * TODO use resource for messages as it is multilangual
     */
    #region Messages
    private const string MsgCannotSavePosition = "Kunne ikke gemme reolens position. Prøv igen.";
    #endregion

    #region variables
    /// <summary>
    /// Number of columns in the shelf grid.
    /// </summary>
    private const int GridColumns = 19;

    /// <summary>
    /// Number of rows in the shelf grid.
    /// Note: Change this if your layout requires a different row count.
    /// </summary>
    private const int GridRows = 28;

    /// <summary>
    /// Fixed cell size in pixels for both columns and rows.
    /// </summary>
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
    // For click vs drag detection
    private DateTime _clickStart;
    private Button? _clickedButton;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes a new instance of the <see cref="ShelfView"/> class.
    /// </summary>
    public ShelfView()
    {
        InitializeComponent();
        // Wait until the UI has loaded to ensure all visual elements are present in the visual tree.
        this.Loaded += ShelfView_Loaded;
    }

    /// <summary>
    /// Handles the view Loaded event: configures the grid shape, wires up drag &amp; drop,
    /// and renders shelves from the database.
    /// </summary>
    /// <param name="sender">The event sender (the <see cref="ShelfView"/>).</param>
    /// <param name="e">The routed event arguments.</param>
    private async void ShelfView_Loaded(object sender, RoutedEventArgs e)
    {
        ConfigureShelfGridShape();
        SetupDragAndDrop();
        await LoadShelvesFromDatabaseAsync();
    }
    #endregion

    #region Data load
    /// <summary>
    /// Loads shelves from persistence and applies grid position and style
    /// (horizontal/vertical) to the matching WPF buttons (e.g., Shelf1..Shelf80).
    /// Missing buttons are created dynamically.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
    /// Initializes drag-and-drop for all shelf buttons, and attaches grid-level fallback events.
    /// </summary>
    private void SetupDragAndDrop()
    {
        // Find all shelf buttons in the grid (recursive visual tree search)
        var shelfButtons = FindVisualChildren<Button>(ShelfGrid).ToList();

        System.Diagnostics.Debug.WriteLine($"Found {shelfButtons.Count} shelf buttons");

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
    /// Begins a potential drag operation for the selected shelf button and stores the initial state.
    /// Distinguishes between click and drag based on movement threshold and timing.
    /// </summary>
    /// <param name="sender">The button that was pressed.</param>
    /// <param name="e">Mouse button event arguments.</param>
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

            // Defer mouse capture and visuals until threshold is passed in MouseMove
            e.Handled = true;
        }
    }

    /// <summary>
    /// Moves the button visually with the mouse during drag (via <see cref="TranslateTransform"/>).
    /// The actual grid position is only updated on drop.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Mouse event arguments.</param>
    private void Button_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var current = e.GetPosition(ShelfGrid);
            // Start drag once we pass the movement threshold
            if (!_isDragging && _draggedButton != null &&
                (Math.Abs(current.X - _dragStartPoint.X) > 5 || Math.Abs(current.Y - _dragStartPoint.Y) > 5))
            {
                _isDragging = true;
                _potentialClick = false;

                // Create a ghost adorner at the button's original location
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
    /// Ends a drag operation, snaps to the nearest visible grid cell, persists the move,
    /// and clears visual state. Also handles quick-click to open shelf info.
    /// </summary>
    /// <param name="sender">The button being dragged.</param>
    /// <param name="e">Mouse button event arguments.</param>
    private async void Button_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedButton != null)
        {
            if (!_isDragging && _potentialClick && _clickedButton == _draggedButton && (DateTime.Now - _clickStart).TotalMilliseconds < 250)
            {
                // Quick click: open info window
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

            // Persist first; rollback on failure
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
    /// <summary>
    /// Opens the shelf info popup for the given shelf button.
    /// </summary>
    /// <param name="button">The shelf button whose number should be displayed.</param>
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

    /// <summary>
    /// Placeholder for mouse leave on a button; no action required (handled by grid-level handlers).
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Mouse event arguments.</param>
    private void Button_MouseLeave(object sender, MouseEventArgs e)
    {
        // No-op; see Grid_MouseMove
    }

    /// <summary>
    /// Grid-level mouse move handler used as a fallback to update the dragged button transform.
    /// </summary>
    /// <param name="sender">The grid.</param>
    /// <param name="e">Mouse event arguments.</param>
    private void Grid_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _draggedButton != null && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(ShelfGrid);

            // Follow the mouse with a RenderTransform
            var delta = currentPosition - _dragStartPoint;
            _draggedButton.RenderTransform = new TranslateTransform(delta.X, delta.Y);

            e.Handled = true;
        }
    }

    /// <summary>
    /// Grid-level mouse left button up handler to finalize a drag, snap to cell and persist.
    /// </summary>
    /// <param name="sender">The grid.</param>
    /// <param name="e">Mouse button event arguments.</param>
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

            // Persist first; rollback on failure
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
    /// <summary>
    /// Attempts to persist a shelf move in the database. On failure, the UI position should be rolled back
    /// to the provided <paramref name="rollbackColumn"/> and <paramref name="rollbackRow"/>.
    /// </summary>
    /// <param name="button">The button representing the shelf.</param>
    /// <param name="targetColumn">Target column (0-based).</param>
    /// <param name="targetRow">Target row (0-based).</param>
    /// <param name="rollbackColumn">Column to roll back to on failure.</param>
    /// <param name="rollbackRow">Row to roll back to on failure.</param>
    /// <returns><c>true</c> if persistence succeeded; otherwise <c>false</c>.</returns>
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
    /// <summary>
    /// Removes the ghost adorner from the adorner layer, if present, and clears references.
    /// </summary>
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
    /// Adorner that draws a semi-transparent visual copy of an element at a fixed position.
    /// Used to visualize the element's original location during drag operations.
    /// </summary>
    private sealed class VisualGhostAdorner : Adorner
    {
        private readonly VisualBrush _brush;
        private readonly double _width;
        private readonly double _height;
        private readonly Point _location;

        /// <summary>
        /// Creates a new instance of <see cref="VisualGhostAdorner"/>.
        /// </summary>
        /// <param name="adornedElement">The element the adorner is attached to (typically the container).</param>
        /// <param name="ghostOf">The element to mirror visually as a ghost.</param>
        /// <param name="location">The top-left position (in container coordinates) where the ghost is drawn.</param>
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

        /// <summary>
        /// Renders the ghost rectangle using a <see cref="VisualBrush"/> of the source element.
        /// </summary>
        /// <param name="drawingContext">The adorner drawing context.</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(_brush, null, new Rect(_location, new Size(_width, _height)));
        }
    }
    #endregion

    #region Grid helpers
    /// <summary>
    /// Computes the column index from an X position (in pixels) relative to <see cref="ShelfGrid"/>.
    /// </summary>
    /// <param name="x">The X coordinate in pixels.</param>
    /// <returns>The zero-based column index.</returns>
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

    /// <summary>
    /// Computes the row index from a Y position (in pixels) relative to <see cref="ShelfGrid"/>.
    /// </summary>
    /// <param name="y">The Y coordinate in pixels.</param>
    /// <returns>The zero-based row index.</returns>
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

    /// <summary>
    /// Returns the effective height for a row definition, with a fallback to absolute height when needed.
    /// </summary>
    /// <param name="row">The row definition.</param>
    /// <returns>The height in pixels.</returns>
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
    /// Sets the grid spans so a shelf occupies two cells horizontally (2x1) or vertically (1x2).
    /// </summary>
    /// <param name="button">The button representing the shelf.</param>
    /// <param name="isHorizontal">If true, the shelf spans 2 columns and 1 row; otherwise 1 column and 2 rows.</param>
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
    /// Clamps the top-left column so the interval [column .. column + spanX - 1] stays within the last visible column.
    /// </summary>
    /// <param name="column">The proposed start column.</param>
    /// <param name="spanX">The horizontal span (number of columns occupied).</param>
    /// <returns>A valid start column after clamping.</returns>
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
    /// Clamps the top-left row so the interval [row .. row + spanY - 1] stays within the last visible row.
    /// </summary>
    /// <param name="row">The proposed start row.</param>
    /// <param name="spanY">The vertical span (number of rows occupied).</param>
    /// <returns>A valid start row after clamping.</returns>
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
    /// <summary>
    /// Enumerates all visual descendants of type <typeparamref name="T"/> under the given <see cref="DependencyObject"/>.
    /// </summary>
    /// <typeparam name="T">The requested type of descendants.</typeparam>
    /// <param name="depObj">The root of the search in the visual tree.</param>
    /// <returns>An enumeration of found elements.</returns>
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
    /// Opens the popup for adding a new shelf.
    /// </summary>
    /// <param name="sender">The button that was clicked.</param>
    /// <param name="e">The routed event arguments.</param>
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

    /// <summary>
    /// Opens the popup for creating a new contract.
    /// </summary>
    /// <param name="sender">The button that was clicked.</param>
    /// <param name="e">The routed event arguments.</param>
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
    /// Handles that a shelf was added via the popup: reloads data and closes the overlay.
    /// </summary>
    /// <param name="sender">The popup.</param>
    /// <param name="e">Event arguments.</param>
    private void OnReolTilfoejet(object? sender, EventArgs e)
    {
        _ = LoadShelvesFromDatabaseAsync();
        LukOverlay();
    }

    /// <summary>
    /// Handles that the popup was canceled and closes the overlay.
    /// </summary>
    /// <param name="sender">The popup.</param>
    /// <param name="e">Event arguments.</param>
    private void OnPopupAnnulleret(object? sender, EventArgs e)
    {
        LukOverlay();
    }

    /// <summary>
    /// Closes the overlay and detaches event handlers from popup components.
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
    /// Enforces a fixed grid of <see cref="GridColumns"/> x <see cref="GridRows"/> with cells of <see cref="CellSizePx"/> pixels,
    /// sizes the grid to match those definitions, and prevents stretching by aligning it to the top-left.
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
