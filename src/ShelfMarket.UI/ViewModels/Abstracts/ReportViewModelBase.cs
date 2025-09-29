using ShelfMarket.UI.ViewModels.Abstracts;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;
//using ShelfMarket.UI.Commands;

//namespace ShelfMarket.UI.ViewModels.Abstracts;

///// <summary>
///// Base class for read-only / analytical “report” style view models (no CRUD).
///// Provides:
/////  - Common state: From / To date (optional range), loading & error handling
/////  - Refresh, Export, and Print commands
/////  - Debounced / non-overlapping async refresh logic
/////  - Simple print pipeline (override <see cref="BuildPrintImage"/>)
/////  - Hook for export data generation
///// Derive from this and implement <see cref="LoadCoreAsync"/> plus optional overrides.
///// </summary>

public abstract class ReportViewModelBase : ModelBase
{
    protected ReportViewModelBase(string title)
    {
        Title = title;
    }
    #region State / Parameters

    //    private bool _isLoading;
    //    /// <summary>True while a refresh (data load) is running.</summary>
    //    public bool IsLoading
    //    {
    //        get => _isLoading;
    //        protected set
    //        {
    //            if (_isLoading == value) return;
    //            _isLoading = value;
    //            OnPropertyChanged();
    //            RaiseCommandStates();
    //        }
    //    }

    //    private string? _error;
    //    /// <summary>Current error message, if any.</summary>
    //    public string? Error
    //    {
    //        get => _error;
    //        protected set
    //        {
    //            if (_error == value) return;
    //            _error = value;
    //            OnPropertyChanged();
    //            OnPropertyChanged(nameof(HasError));
    //            RaiseCommandStates();
    //        }
    //    }

    //    /// <summary>True when an error message is present.</summary>
    //    public bool HasError => !string.IsNullOrEmpty(Error);

    //    private bool _useDateRange;
    //    /// <summary>
    //    /// When false the report treats only <see cref="FromDate"/> as the active date (single-day mode).
    //    /// When true it uses the inclusive range [FromDate..ToDate].
    //    /// </summary>
    //    public bool UseDateRange
    //    {
    //        get => _useDateRange;
    //        set
    //        {
    //            if (_useDateRange == value) return;
    //            _useDateRange = value;
    //            OnPropertyChanged();
    //            if (AutoRefreshOnParameterChange) _ = RefreshAsync();
    //        }
    //    }

    //    /// <summary>
    //    /// If true, changing parameter properties (dates / range flag) triggers an automatic refresh.
    //    /// Defaults to true.
    //    /// </summary>
    //    public bool AutoRefreshOnParameterChange { get; set; } = true;

    //    private bool _canPrint;
    //    /// <summary>Indicates whether a printable image is currently available.</summary>
    //    public bool CanPrint
    //    {
    //        get => _canPrint;
    //        protected set
    //        {
    //            if (_canPrint == value) return;
    //            _canPrint = value;
    //            OnPropertyChanged();
    //            RaiseCommandStates();
    //        }
    //    }
    #endregion

    #region Properties
    private string _title;

    public string Title
    {
        get { return _title; }
        init { _title = value; }
    }
    private DateTime _fromDate = DateTime.Today;
    /// <summary>Starting date (inclusive) for the report.</summary>
    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate == value) return;
            _fromDate = value.Date;
            OnPropertyChanged();
            //if (AutoRefreshOnParameterChange) _ = RefreshAsync();
        }
    }

    private DateTime _toDate = DateTime.Today;
    /// <summary>Ending date (inclusive) for the report.</summary>
    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (_toDate == value) return;
            _toDate = value.Date;
            OnPropertyChanged();
            //if (AutoRefreshOnParameterChange) _ = RefreshAsync();
        }
    }

    #endregion


    #region Commands
    //    public ICommand RefreshCommand { get; }
    //    public ICommand ExportCommand { get; }
    //    public ICommand PrintCommand { get; }
    //    #endregion

    //    #region Concurrency
    //    private readonly object _loadLock = new();
    //    private CancellationTokenSource? _cts;
    //    #endregion

    //    /// <summary>
    //    /// Initializes the base report VM and wires commands.
    //    /// </summary>
    //    protected ReportViewModelBase()
    //    {
    //        RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsLoading);
    //        ExportCommand = new RelayCommand(async () => await ExportAsync(), () => !IsLoading && !HasError && CanExport());
    //        PrintCommand = new RelayCommand(() => ExecutePrint(), () => CanPrint);
    //    }

    //    #region Public API

    //    /// <summary>
    //    /// Manually triggers data reload (cancels an in-progress load first).
    //    /// </summary>
    //    public async Task RefreshAsync()
    //    {
    //        if (!ValidateParameters())
    //            return;

    //        CancellationTokenSource? toCancel = null;
    //        CancellationToken token;

    //        lock (_loadLock)
    //        {
    //            if (_cts != null)
    //            {
    //                toCancel = _cts;
    //            }
    //            _cts = new CancellationTokenSource();
    //            token = _cts.Token;
    //        }

    //        toCancel?.Cancel();
    //        toCancel?.Dispose();

    //        IsLoading = true;
    //        Error = null;

    //        try
    //        {
    //            await LoadCoreAsync(token);
    //            PostLoad();
    //        }
    //        catch (OperationCanceledException)
    //        {
    //            // Swallow - user triggered another refresh
    //        }
    //        catch (Exception ex)
    //        {
    //            Error = ex.Message;
    //        }
    //        finally
    //        {
    //            IsLoading = false;
    //            RefreshPrintState();
    //        }
    //    }

    //    /// <summary>
    //    /// Invokes export logic (override <see cref="BuildExportPayload"/> to provide data).
    //    /// Default implementation places the object on clipboard (if any) as plain text JSON.
    //    /// Replace with file-save logic as needed.
    //    /// </summary>
    //    protected virtual async Task ExportAsync()
    //    {
    //        try
    //        {
    //            var payload = await BuildExportPayload();
    //            if (payload is null) return;

    //            // Simple default: serialize to JSON (if System.Text.Json is available) & copy to clipboard
    //            try
    //            {
    //                var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
    //                {
    //                    WriteIndented = true
    //                });
    //                System.Windows.Clipboard.SetText(json);
    //                InfoMessage = "Rapport export kopieret til udklipsholder.";
    //            }
    //            catch (Exception ex)
    //            {
    //                Error = "Export mislykkedes: " + ex.Message;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Error = ex.Message;
    //        }
    //        await Task.CompletedTask;
    //    }

    #endregion

    #region Hooks (override in derived classes)

    //    /// <summary>
    //    /// Perform the actual data loading. Throwing sets <see cref="Error"/>.
    //    /// Use <paramref name="ct"/> for cancellation.
    //    /// </summary>
    //    protected abstract Task LoadCoreAsync(CancellationToken ct);

    //    /// <summary>
    //    /// Optional post-processing hook executed after successful load.
    //    /// </summary>
    //    protected virtual void PostLoad() { }

    //    /// <summary>
    //    /// Build an object representing the export (CSV rows list, DTO graph, etc.).
    //    /// Return null to indicate no export available.
    //    /// </summary>
    //    protected virtual Task<object?> BuildExportPayload() => Task.FromResult<object?>(null);

    //    /// <summary>
    //    /// Determines whether exporting is currently meaningful (e.g. data present).
    //    /// </summary>
    //    protected virtual bool CanExport() => !HasError;

    //    /// <summary>
    //    /// Creates an image for printing. Return null to disable print.
    //    /// Override to draw onto a DrawingVisual or return an existing ImageSource.
    //    /// </summary>
    //    protected virtual ImageSource? BuildPrintImage() => null;

    #endregion

    #region Printing

    //    private void ExecutePrint()
    //    {
    //        var img = BuildPrintImage();
    //        if (img is not BitmapSource bmp) return;

    //        var dlg = new PrintDialog();
    //        if (dlg.ShowDialog() != true) return;

    //        double targetW = dlg.PrintableAreaWidth;
    //        double targetH = dlg.PrintableAreaHeight;

    //        // Convert pixel size to DIU (96 DPI baseline)
    //        double imgW = bmp.PixelWidth * (96.0 / bmp.DpiX);
    //        double imgH = bmp.PixelHeight * (96.0 / bmp.DpiY);

    //        double scale = Math.Min(targetW / imgW, targetH / imgH);
    //        double drawW = imgW * scale;
    //        double drawH = imgH * scale;
    //        double ox = (targetW - drawW) / 2;
    //        double oy = (targetH - drawH) / 2;

    //        var dv = new DrawingVisual();
    //        using (var dc = dv.RenderOpen())
    //        {
    //            dc.DrawRectangle(Brushes.White, null, new System.Windows.Rect(0, 0, targetW, targetH));
    //            dc.DrawImage(bmp, new System.Windows.Rect(ox, oy, drawW, drawH));
    //        }
    //        dlg.PrintVisual(dv, "Report");
    //    }

    //    /// <summary>
    //    /// Re-evaluates print availability by calling <see cref="BuildPrintImage"/>.
    //    /// </summary>
    //    protected void RefreshPrintState()
    //    {
    //        CanPrint = BuildPrintImage() != null;
    //    }

    #endregion

    #region Helpers

    //    /// <summary>
    //    /// Override for custom parameter validation. Set <see cref="Error"/> and return false to abort refresh.
    //    /// </summary>
    //    protected virtual bool ValidateParameters()
    //    {
    //        if (UseDateRange && FromDate > ToDate)
    //        {
    //            Error = "Fra-dato må ikke være efter Til-dato.";
    //            return false;
    //        }
    //        return true;
    //    }

    //    private void RaiseCommandStates()
    //    {
    //        (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
    //        (ExportCommand as RelayCommand)?.RaiseCanExecuteChanged();
    //        (PrintCommand as RelayCommand)?.RaiseCanExecuteChanged();
    //    }

    //    /// <summary>
    //    /// Convenience for derived classes to set an informational message (not persisted here,
    //    /// but you can add property if desired).
    //    /// </summary>
    //    protected string? InfoMessage
    //    {
    //        get => _infoMessage;
    //        set
    //        {
    //            if (_infoMessage == value) return;
    //            _infoMessage = value;
    //            OnPropertyChanged();
    //        }
    //    }

    #endregion

    #region Disposal (optional extension)
    //    /// <summary>
    //    /// Call when the view model is no longer needed to cancel ongoing work.
    //    /// </summary>
    //    public virtual void Dispose()
    //    {
    //        lock (_loadLock)
    //        {
    //            _cts?.Cancel();
    //            _cts?.Dispose();
    //            _cts = null;
    //        }
    //    }
    #endregion
}