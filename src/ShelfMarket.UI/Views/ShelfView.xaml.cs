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
using Microsoft.Extensions.DependencyInjection;
using ShelfMarket.Application.Abstracts.Services;

namespace ShelfMarket.UI.Views
{
	/// <summary>
	/// Brugerflade til visualisering og manuel placering af reoler i et grid.
	/// Understøtter drag-and-drop mellem synlige celler samt popup til oprettelse af nye reoler.
	/// </summary>
	public partial class ShelfView : UserControl
	{
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

					// Clamp to visible grid range in case DB holds out-of-range coordinates
					var maxCol = Math.Max(0, ShelfGrid.ColumnDefinitions.Count - 1);
					var maxRow = Math.Max(0, ShelfGrid.RowDefinitions.Count - 1);
					var col = Math.Min(Math.Max(0, shelf.LocationX), maxCol);
					var row = Math.Min(Math.Max(0, shelf.LocationY), maxRow);

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
		private async Task EnsureMissingShelvesCreatedFromUIAsync(IEnumerable<ShelfMarket.Domain.Entities.Shelf> existing)
		{
			try
			{
				var existingByNumber = existing.ToDictionary(s => s.Number, s => s);
				var allButtons = FindVisualChildren<Button>(ShelfGrid).Where(b => b.Name.StartsWith("Shelf")).ToList();

				// Resolve style references once
				Style? styleH = null, styleV = null;
				try
				{
					styleH = FindResource("Stand.Horizontal") as Style;
					styleV = FindResource("Stand.Vertical") as Style;
				}
				catch { }

				var missing = new List<(int Number, int Col, int Row, bool IsHorizontal)>();
				var updates = new List<(ShelfMarket.Domain.Entities.Shelf Shelf, int Col, int Row, bool? NewHorizontal)>();

				foreach (var b in allButtons)
				{
					if (!int.TryParse(b.Name["Shelf".Length..], out var num)) continue;
					int col = Grid.GetColumn(b);
					int row = Grid.GetRow(b);
					bool isHorizontal = false;
					try
					{
						if (styleH != null && ReferenceEquals(b.Style, styleH)) isHorizontal = true;
						else if (styleV != null && ReferenceEquals(b.Style, styleV)) isHorizontal = false;
					}
					catch { }

					if (existingByNumber.TryGetValue(num, out var shelf))
					{
						bool needUpdatePos = shelf.LocationX != col || shelf.LocationY != row;
						bool needUpdateOri = shelf.OrientationHorizontal != isHorizontal;
						if (needUpdatePos || needUpdateOri)
							updates.Add((shelf, col, row, needUpdateOri ? isHorizontal : (bool?)null));
					}
					else
					{
						missing.Add((num, col, row, isHorizontal));
					}
				}

				// DB er nu source of truth – ingen automatisk reset længere
				return;

				using var scope = App.HostInstance.Services.CreateScope();
				var db = scope.ServiceProvider.GetRequiredService<ShelfMarket.Infrastructure.Persistence.ShelfMarketDbContext>();
				var type = db.ShelfTypes.FirstOrDefault();
				var typeId = type?.Id ?? Guid.NewGuid();
				if (type == null)
				{
					db.ShelfTypes.Add(new ShelfMarket.Domain.Entities.ShelfType { Id = typeId, Name = "Default" });
				}
				foreach (var (shelf, col, row, newH) in updates)
				{
					shelf.LocationX = col;
					shelf.LocationY = row;
					if (newH.HasValue) shelf.OrientationHorizontal = newH.Value;
				}
				foreach (var m in missing)
				{
					db.Shelves.Add(new ShelfMarket.Domain.Entities.Shelf
					{
						Id = Guid.NewGuid(),
						Number = m.Number,
						ShelfTypeId = typeId,
						LocationX = m.Col,
						LocationY = m.Row,
						OrientationHorizontal = m.IsHorizontal
					});
				}
				await db.SaveChangesAsync();
			}
			catch
			{
				// ignore – seeding er kun convenience
			}
		}

		private static string shellfNumber(uint number) => number.ToString();

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
				// Snap til nærmeste synlige celle ved drop
				var releasePosition = e.GetPosition(ShelfGrid);
				var targetColumn = ClampToVisibleColumns(GetColumnFromPosition(releasePosition.X));
				var targetRow = ClampToVisibleRows(GetRowFromPosition(releasePosition.Y));
				// Persistér først; hvis gem fejler, ruller vi tilbage
				var button = _draggedButton; // snapshot to avoid null mid-cleanup
				if (button == null) { e.Handled = true; return; }
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
				
				// Markér event som håndteret
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
		private async void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_isDragging && _draggedButton != null)
			{
				System.Diagnostics.Debug.WriteLine($"Grid Mouse up - Slut drag operation");
				
				_isDragging = false;
				// Snap til nærmeste synlige celle
				var releasePosition = e.GetPosition(ShelfGrid);
				var targetColumn = ClampToVisibleColumns(GetColumnFromPosition(releasePosition.X));
				var targetRow = ClampToVisibleRows(GetRowFromPosition(releasePosition.Y));
				// Persistér først; hvis gem fejler, ruller vi tilbage
				var button = _draggedButton; // snapshot
				if (button == null) { e.Handled = true; return; }
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
				
				// Markér event som håndteret
				e.Handled = true;
			}
		}
		#endregion

		#region Persistence
		/// <summary>
		/// Attempts to persist the move. Returns true if stored successfully.
		/// Occupied target cells are handled silently (UI will rollback without a popup).
		/// Unexpected errors still surface as a message so we can troubleshoot.
		/// </summary>
		private async Task<bool> TryPersistMoveAsync(Button button, int targetColumn, int targetRow, int rollbackColumn, int rollbackRow)
		{
			try
			{
				var numberStr = button.Name.StartsWith("Shelf") ? button.Name["Shelf".Length..] : string.Empty;
				if (!int.TryParse(numberStr, out var number)) return false;

				bool isHorizontal = false;
				try
				{
					var h = FindResource("Stand.Horizontal") as Style;
					if (h != null && ReferenceEquals(button.Style, h)) isHorizontal = true;
					else
					{
						var v = FindResource("Stand.Vertical") as Style;
						if (v != null && ReferenceEquals(button.Style, v) == false) isHorizontal = true; // fallback guess
					}
				}
				catch { }

				using var scope = App.HostInstance.Services.CreateScope();
				var layoutService = scope.ServiceProvider.GetRequiredService<IShelfLayoutService>();
				// Persist the move using the application service
				// Service ensures target is free and updates orientation if changed
				var ok = await layoutService.TryUpdatePositionAsync(number, targetColumn, targetRow, isHorizontal);
				if (!ok)
				{
					// Diagnose common causes to help the user
					try
					{
						var all = await layoutService.GetAllAsync();
						var shelf = all.FirstOrDefault(s => s.Number == number);
						bool shelfExists = shelf != null;
						bool occupied = all.Any(s => s.LocationX == targetColumn && s.LocationY == targetRow && s.Number != number);
						if (!shelfExists)
						{
							MessageBox.Show($"Reol {number} findes ikke i databasen endnu. Jeg opretter den nu ved første flyt.", "Manglende reol", MessageBoxButton.OK, MessageBoxImage.Information);
							// Create on first move using default shelf type
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
								// ignore, fall back to rollback
							}
						}
						else if (occupied)
						{
							// Occupied target: silent rollback – no popup desired
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
		#endregion

		#region Grid helpers
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
		#endregion

		#region Visual tree + overlay/popups
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
			// Refresh shelves from DB so the newly created shelf appears immediately
			_ = LoadShelvesFromDatabaseAsync();

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
		#endregion
	}
}
