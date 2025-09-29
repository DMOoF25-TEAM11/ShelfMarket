using System.Collections.ObjectModel;
using System.Windows.Controls;
using ShelfMarket.UI.Models;

namespace ShelfMarket.UI.ViewModels;

public class CashReportViewModel : ViewBase
{
    //    private readonly ICashReportService _service;

    public ObservableCollection<CashDenomination> Denominations { get; } = new();

    private DateTime _date = DateTime.Today;
    public DateTime Date
    {
        get => _date;
        set
        {
            if (_date == value) return;
            _date = value;
            //OnPropertyChanged();
            //_ = LoadAsync();
        }
    }















    //    private void Recalculate()
    //    {
    //        CountedCash = OpeningCash + Denominations.Sum(d => d.Amount);
    //        ExpectedCash = OpeningCash + CashSalesSystem;
    //        OnPropertyChanged(nameof(Difference));
    //        _cachedImage = null;
    //        RefreshPrintState();
    //    }

    //    protected override ImageSource? GetImageToPrint()
    //    {
    //        if (_cachedImage != null) return _cachedImage;

    //        // A4 @ 96 DPI
    //        double width = 96.0 / 25.4 * 210;   // 793.7
    //        double height = 96.0 / 25.4 * 297;  // 1122.5
    //        var dv = new DrawingVisual();
    //        using (var dc = dv.RenderOpen())
    //        {
    //            var rect = new Rect(0, 0, width, height);
    //            dc.DrawRectangle(Brushes.White, null, rect);

    //            double margin = 40;
    //            double x = margin;
    //            double y = margin;

    //            var typeHeader = new Typeface("Segoe UI Semibold");
    //            var typeNormal = new Typeface("Segoe UI");
    //            double fsHeader = 20;
    //            double fs = 14;

    //            void DrawText(string text, double tx, double ty, double size, Typeface tf, Brush? brush = null)
    //            {
    //                var ft = new FormattedText(
    //                    text,
    //#if NET9_0_OR_GREATER
    //                    System.Globalization.CultureInfo.CurrentCulture,
    //#else
    //                    System.Globalization.CultureInfo.CurrentCulture,
    //#endif
    //                    FlowDirection.LeftToRight,
    //                    tf,
    //                    size,
    //                    brush ?? Brushes.Black,
    //                    1.25);
    //                dc.DrawText(ft, new Point(tx, ty));
    //            }

    //            DrawText("Kasserapport", x, y, fsHeader, typeHeader);
    //            y += fsHeader + 10;
    //            DrawText($"Dato: {IssuedAt:yyyy-MM-dd}", x, y, fs, typeNormal);
    //            y += fs + 4;

    //            DrawText($"Åbningsbeholdning: {OpeningCash:C}", x, y, fs, typeNormal);
    //            y += fs + 2;
    //            DrawText($"Kontantsalg (system): {CashSalesSystem:C}", x, y, fs, typeNormal);
    //            y += fs + 2;
    //            DrawText($"Forventet kontantbeholdning: {ExpectedCash:C}", x, y, fs, typeNormal);
    //            y += fs + 10;

    //            // Table headers
    //            double tableX = x;
    //            double colDenom = 160;
    //            double colCount = 120;
    //            double colAmount = 160;
    //            double rowH = fs + 6;

    //            DrawText("Værdi", tableX, y, fs, typeHeader);
    //            DrawText("Antal", tableX + colDenom, y, fs, typeHeader);
    //            DrawText("Beløb", tableX + colDenom + colCount, y, fs, typeHeader);
    //            y += rowH;

    //            foreach (var d in Denominations.OrderByDescending(d => d.Value))
    //            {
    //                DrawText(d.Value.ToString("C"), tableX, y, fs, typeNormal);
    //                DrawText(d.Count.ToString(), tableX + colDenom, y, fs, typeNormal);
    //                DrawText(d.Amount.ToString("C"), tableX + colDenom + colCount, y, fs, typeNormal);
    //                y += rowH;
    //            }

    //            y += 4;
    //            dc.DrawLine(new Pen(Brushes.Black, 1), new Point(tableX, y), new Point(tableX + colDenom + colCount + colAmount, y));
    //            y += 6;

    //            DrawText($"Optalt kontant: {CountedCash:C}", tableX, y, fs, typeHeader);
    //            y += fs + 4;
    //            var diffBrush = Difference == 0 ? Brushes.DarkGreen :
    //                Difference > 0 ? Brushes.DarkOrange : Brushes.DarkRed;
    //            DrawText($"Difference: {Difference:C}", tableX, y, fs, typeHeader, diffBrush);
    //            y += fs + 12;

    //            if (!string.IsNullOrWhiteSpace(Note))
    //            {
    //                DrawText("Notat:", tableX, y, fs, typeHeader);
    //                y += fs + 4;
    //                var noteLines = BreakLines(Note!, 80);
    //                foreach (var line in noteLines)
    //                {
    //                    DrawText(line, tableX, y, fs, typeNormal);
    //                    y += fs + 2;
    //                }
    //                y += 10;
    //            }

    //            // Signature section
    //            double sigTop = height - margin - 140;
    //            double sigWidth = (width - margin * 2) / 3.0 - 20;
    //            void DrawSignatureBox(double sx, string label)
    //            {
    //                double boxH = 70;
    //                var r = new Rect(sx, sigTop, sigWidth, boxH);
    //                dc.DrawRectangle(null, new Pen(Brushes.Black, 1), r);
    //                DrawText(label, sx + 6, sigTop + boxH + 6, fs, typeNormal);
    //            }

    //            DrawSignatureBox(x, "Kasserer underskrift");
    //            DrawSignatureBox(x + sigWidth + 30, "Dato / Tid");
    //            DrawSignatureBox(x + (sigWidth + 30) * 2, "Kontrol underskrift");
    //        }

    //        var bmp = new RenderTargetBitmap(
    //            (int)Math.Ceiling(width),
    //            (int)Math.Ceiling(height),
    //            96, 96,
    //            PixelFormats.Pbgra32);
    //        bmp.Render(dv);
    //        _cachedImage = bmp;
    //        return _cachedImage;
    //    }

    //    private static IEnumerable<string> BreakLines(string text, int max)
    //    {
    //        if (string.IsNullOrEmpty(text))
    //            yield break;

    //        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    //        var current = new List<string>();
    //        foreach (var w in words)
    //        {
    //            if (string.Join(' ', current.Append(w)).Length > max && current.Count > 0)
    //            {
    //                yield return string.Join(' ', current);
    //                current.Clear();
    //            }
    //            current.Add(w);
    //        }
    //        if (current.Count > 0)
    //            yield return string.Join(' ', current);
    //    }
}