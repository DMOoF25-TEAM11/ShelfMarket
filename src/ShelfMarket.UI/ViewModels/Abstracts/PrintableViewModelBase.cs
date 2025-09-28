using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShelfMarket.UI.Commands;

namespace ShelfMarket.UI.ViewModels.Abstracts;

/// <summary>
/// Base view model that supplies a print command for derived view models which
/// expose an <see cref="ImageSource"/> to be printed.
/// </summary>
/// <remarks>
/// Printing logic automatically scales (uniformly) and centers the bitmap inside the
/// printer's printable area while preserving aspect ratio. Legacy label size properties
/// (<see cref="LabelWidthMm"/>, <see cref="LabelHeightMm"/>) are retained for backwards
/// compatibility (e.g., existing bindings) but are no longer used in the print routine.
/// </remarks>
public abstract class PrintableViewModelBase : ModelBase
{
    /// <summary>
    /// Backing field for <see cref="LabelWidthMm"/>.
    /// Kept for backward compatibility with older bindings; not used in print calculations.
    /// </summary>
    private double _labelWidthMm = 58;

    /// <summary>
    /// Legacy label width in millimeters (not used during printing). Updating the value
    /// raises <see cref="ModelBase.PropertyChanged"/> for any bound UI elements.
    /// </summary>
    public double LabelWidthMm
    {
        get => _labelWidthMm;
        set
        {
            if (Math.Abs(_labelWidthMm - value) < 0.01) return;
            _labelWidthMm = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Backing field for <see cref="LabelHeightMm"/>.
    /// Kept for backward compatibility with older bindings; not used in print calculations.
    /// </summary>
    private double _labelHeightMm = 30;

    /// <summary>
    /// Legacy label height in millimeters (not used during printing). Updating the value
    /// raises <see cref="ModelBase.PropertyChanged"/>.
    /// </summary>
    public double LabelHeightMm
    {
        get => _labelHeightMm;
        set
        {
            if (Math.Abs(_labelHeightMm - value) < 0.01) return;
            _labelHeightMm = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Backing field for <see cref="CanPrint"/>.
    /// </summary>
    private bool _canPrint;

    /// <summary>
    /// Indicates whether a printable image is currently available. When this value changes,
    /// the <see cref="PrintCommand"/> CanExecute state is refreshed.
    /// </summary>
    public bool CanPrint
    {
        get => _canPrint;
        protected set
        {
            if (_canPrint == value) return;
            _canPrint = value;
            OnPropertyChanged();
            (PrintCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Command that initiates printing of the current image returned by <see cref="GetImageToPrint"/>
    /// </summary>
    public ICommand PrintCommand { get; }

    /// <summary>
    /// Initializes a new instance and wires the <see cref="PrintCommand"/>.
    /// </summary>
    protected PrintableViewModelBase()
    {
        PrintCommand = new RelayCommand(ExecutePrint, () => CanPrint);
    }

    /// <summary>
    /// Returns the image (typically a barcode, label, or other visual) to be printed.
    /// Implementations should return <c>null</c> when no print-ready image is available.
    /// </summary>
    /// <returns>An <see cref="ImageSource"/> if printable content exists; otherwise <c>null</c>.</returns>
    protected abstract ImageSource? GetImageToPrint();

    /// <summary>
    /// Reevaluates whether printing is currently possible by checking if <see cref="GetImageToPrint"/>
    /// returns a non-null value. Updates <see cref="CanPrint"/>.
    /// </summary>
    protected void RefreshPrintState()
    {
        CanPrint = GetImageToPrint() != null;
    }

    /// <summary>
    /// Executes the print workflow:
    /// 1. Retrieves the current image.
    /// 2. Shows a standard <see cref="PrintDialog"/>.
    /// 3. Scales and centers the image within the printable area while preserving aspect ratio.
    /// </summary>
    /// <remarks>
    /// If no image is available or the user cancels the dialog, the method returns without action.
    /// </remarks>
    private void ExecutePrint()
    {
        if (GetImageToPrint() is not BitmapSource img) return;

        var dlg = new PrintDialog();
        if (dlg.ShowDialog() != true) return;

        double targetW = dlg.PrintableAreaWidth;
        double targetH = dlg.PrintableAreaHeight;

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            // Background (white) to ensure predictable print output
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, targetW, targetH));

            // Convert pixel size to device-independent units (DIU) using 96 DPI baseline.
            double imgW = img.PixelWidth * (96.0 / img.DpiX);
            double imgH = img.PixelHeight * (96.0 / img.DpiY);

            // Uniform scale to fit inside printable area
            double scale = Math.Min(targetW / imgW, targetH / imgH);
            double drawW = imgW * scale;
            double drawH = imgH * scale;

            // Center offsets
            double ox = (targetW - drawW) / 2;
            double oy = (targetH - drawH) / 2;

            dc.DrawImage(img, new Rect(ox, oy, drawW, drawH));
        }

        dlg.PrintVisual(dv, "Label");
    }
}