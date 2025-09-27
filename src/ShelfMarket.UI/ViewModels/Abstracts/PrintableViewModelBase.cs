using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ShelfMarket.UI.Commands;

namespace ShelfMarket.UI.ViewModels.Abstracts;

/// <summary>
/// Base view model providing a print command for any derived view model
/// that exposes an image to print. Printing now auto-fits the image
/// to the printable area (label size properties kept only for backward compatibility).
/// </summary>
public abstract class PrintableViewModelBase : ModelBase
{
    // Retained for backward compatibility but no longer used during printing.
    private double _labelWidthMm = 58;
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

    private double _labelHeightMm = 30;
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

    private bool _canPrint;
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

    public ICommand PrintCommand { get; }

    protected PrintableViewModelBase()
    {
        PrintCommand = new RelayCommand(ExecutePrint, () => CanPrint);
    }

    protected abstract ImageSource? GetImageToPrint();

    protected void RefreshPrintState()
    {
        CanPrint = GetImageToPrint() != null;
    }

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
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, targetW, targetH));

            // Size of the bitmap in DIU
            double imgW = img.PixelWidth * (96.0 / img.DpiX);
            double imgH = img.PixelHeight * (96.0 / img.DpiY);

            double scale = Math.Min(targetW / imgW, targetH / imgH);
            double drawW = imgW * scale;
            double drawH = imgH * scale;
            double ox = (targetW - drawW) / 2;
            double oy = (targetH - drawH) / 2;

            dc.DrawImage(img, new Rect(ox, oy, drawW, drawH));
        }

        dlg.PrintVisual(dv, "Label");
    }
}