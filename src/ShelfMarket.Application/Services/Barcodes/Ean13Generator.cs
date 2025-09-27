using System.Globalization;
using ShelfMarket.Application.Abstract.Services.Barcodes;
using SkiaSharp;

namespace ShelfMarket.Application.Services.Barcodes;

// EAN-13 generator where the first part is the shelf number and the last part is the price (in cents).
// Defaults: shelf=6 digits, price=6 digits -> 12 data digits + 1 check digit.
public sealed class Ean13BarcodeGenerator : IEan13Generator
{
    // L (odd), G (even), R patterns
    private static readonly string[] L =
    [
        "0001101","0011001","0010011","0111101","0100011",
        "0110001","0101111","0111011","0110111","0001011"
    ];

    private static readonly string[] G =
    [
        "0100111","0110011","0011011","0100001","0011101",
        "0111001","0000101","0010001","0001001","0010111"
    ];

    private static readonly string[] R =
    [
        "1110010","1100110","1101100","1000010","1011100",
        "1001110","1010000","1000100","1001000","1110100"
    ];

    // Parity pattern for the left-side 6 digits decided by the first (number system) digit
    private static readonly string[] ParityByFirstDigit =
    [
        "LLLLLL","LLGLGG","LLGGLG","LLGGGL","LGLLGG",
        "LGGLLG","LGGGLL","LGLGLG","LGLGGL","LGGLGL",
    ];

    public string ComposeData12(string shelfNumberDigits, decimal price, int shelfDigits = 6, int priceDigits = 6)
    {
        if (shelfDigits < 1 || priceDigits < 1) throw new ArgumentOutOfRangeException("Digits must be >= 1.");
        if (shelfDigits + priceDigits != 12)
            throw new ArgumentException("shelfDigits + priceDigits must equal 12 for EAN-13 data.");

        var shelf = NormalizeDigits(shelfNumberDigits);
        var cents = ToCents(price);

        if (shelf.Length > shelfDigits)
            throw new ArgumentException($"Shelf number has more than {shelfDigits} digits.", nameof(shelfNumberDigits));

        if (cents.Length > priceDigits)
            throw new ArgumentException($"Price (in cents) has more than {priceDigits} digits.", nameof(price));

        shelf = shelf.PadLeft(shelfDigits, '0');
        cents = cents.PadLeft(priceDigits, '0');

        return shelf + cents; // 12 data digits
    }

    public int ComputeCheckDigit(string data12)
    {
        if (string.IsNullOrWhiteSpace(data12) || data12.Length != 12 || !data12.All(char.IsDigit))
            throw new ArgumentException("data12 must be exactly 12 digits.", nameof(data12));

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = data12[i] - '0';
            // EAN-13: positions are 1-based; even positions weight 3
            sum += ((i + 1) % 2 == 0) ? digit * 3 : digit;
        }
        int mod = sum % 10;
        return (10 - mod) % 10;
    }

    public string Build(string shelfNumberDigits, decimal price, int shelfDigits = 6, int priceDigits = 6)
    {
        string data12 = ComposeData12(shelfNumberDigits, price, shelfDigits, priceDigits);
        int check = ComputeCheckDigit(data12);
        return data12 + check.ToString(CultureInfo.InvariantCulture);
    }

    public byte[] RenderPng(string ean13, int scale = 3, int barHeight = 60, bool includeNumbers = true)
    {
        ValidateEan13(ean13);

        if (scale < 1) throw new ArgumentOutOfRangeException(nameof(scale));
        if (barHeight < 10) throw new ArgumentOutOfRangeException(nameof(barHeight), "barHeight should be >= 10.");

        string modules = EncodeModules(ean13);

        // Parse shelf / price (defaults: 6 + 6)
        const int shelfDigits = 6;
        const int priceDigits = 6;
        string shelfPart = ean13.Substring(0, shelfDigits);
        string pricePart = ean13.Substring(shelfDigits, priceDigits);
        if (!long.TryParse(pricePart, out var priceCents))
            throw new ArgumentException("Unable to parse price digits.", nameof(ean13));
        decimal price = priceCents / 100m;

        // Quiet zones (10 modules on each side recommended)
        int quiet = 10;
        int totalModules = quiet + modules.Length + quiet;

        int width = totalModules * scale;

        // We will always add the extra lines (Reol / pris)
        int logicalLines = (includeNumbers ? 1 : 0) + 2;
        float fontSizePx = 12f;
        using var tmpTypeface = ResolveTypeface(new[] { "Segoe UI", "Arial", "Liberation Sans", "DejaVu Sans", "Noto Sans", "Ubuntu" });
        using SKFont tmpFont = new(tmpTypeface, fontSizePx);
        tmpFont.GetFontMetrics(out var tmpMetrics);
        float singleLineHeight = tmpMetrics.Descent - tmpMetrics.Ascent;
        float lineSpacing = 2f;
        float totalTextHeight = logicalLines * singleLineHeight + (logicalLines - 1) * lineSpacing;
        int numberArea = (int)Math.Ceiling(totalTextHeight + 4);
        int height = barHeight + numberArea;

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        // Draw bars
        using (var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = false,
            Style = SKPaintStyle.Fill
        })
        {
            int x = quiet * scale;
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == '1')
                {
                    var rect = new SKRect(x, 0, x + scale, barHeight);
                    canvas.DrawRect(rect, paint);
                }
                x += scale;
            }
        }

        // Draw text block (digits + Reol + pris)
        using var typeface = ResolveTypeface(new[] { "Segoe UI", "Arial", "Liberation Sans", "DejaVu Sans", "Noto Sans", "Ubuntu" });
        using SKFont font = new(typeface, fontSizePx);
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        font.GetFontMetrics(out var metrics);
        float textHeight = metrics.Descent - metrics.Ascent;

        var culture = new CultureInfo("da-DK");

        // Remove leading zeros from shelf (keep at least one digit)
        string shelfDisplayRaw = shelfPart.TrimStart('0');
        string shelfDisplay = string.IsNullOrEmpty(shelfDisplayRaw) ? "0" : shelfDisplayRaw;

        string priceDisplay = price.ToString("0.00", culture);

        var lines = new List<(string text, bool center)>
        {
            // Only include the raw EAN digits line if requested
        };
        if (includeNumbers)
            lines.Add((ean13, true));

        // Add requested custom lines
        lines.Add(($"Reol: {shelfDisplay}", false));
        lines.Add(($"pris {priceDisplay} kr,", false));

        float currentBaseline = barHeight + 2 - metrics.Ascent;

        foreach (var (text, center) in lines)
        {
            float textWidth = font.MeasureText(text, textPaint);
            float tx = center ? (width - textWidth) / 2f : 4f;
            canvas.DrawText(text, tx, currentBaseline, SKTextAlign.Left, font, textPaint);
            currentBaseline += textHeight + lineSpacing;
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public Task<byte[]> RenderPngAsync(string ean13, int scale = 3, int barHeight = 60, bool includeNumbers = true, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RenderPng(ean13, scale, barHeight, includeNumbers);
        }, cancellationToken);
    }

    private static void ValidateEan13(string ean13)
    {
        if (string.IsNullOrWhiteSpace(ean13) || ean13.Length != 13 || !ean13.All(char.IsDigit))
            throw new ArgumentException("ean13 must be exactly 13 digits.", nameof(ean13));

        string data12 = ean13[..12];
        int expected = new Ean13BarcodeGenerator().ComputeCheckDigit(data12);
        int actual = ean13[12] - '0';
        if (expected != actual)
            throw new ArgumentException("Invalid EAN-13: check digit mismatch.", nameof(ean13));
    }

    private static string NormalizeDigits(string raw)
    {
        if (raw == null) return string.Empty;
        return new string(raw.Where(char.IsDigit).ToArray());
    }

    private static string ToCents(decimal price)
    {
        // round to 2 decimals, then format as integer cents
        int cents = (int)Math.Round(price * 100m, MidpointRounding.AwayFromZero);
        return Math.Abs(cents).ToString(CultureInfo.InvariantCulture);
    }

    private static string EncodeModules(string ean13)
    {
        // EAN-13 total modules (excluding quiet zones): 95
        // Structure: 3 (start) + 42 (left 6 digits) + 5 (middle) + 42 (right 6 digits) + 3 (end)
        int first = ean13[0] - '0';
        string parity = ParityByFirstDigit[first];

        // Start guard
        var modules = "101";

        // Left six digits (positions 2..7 in the EAN string)
        for (int i = 1; i <= 6; i++)
        {
            int d = ean13[i] - '0';
            bool useG = parity[i - 1] == 'G';
            modules += useG ? G[d] : L[d];
        }

        // Middle guard
        modules += "01010";

        // Right six digits (positions 8..13)
        for (int i = 7; i < 13; i++)
        {
            int d = ean13[i] - '0';
            modules += R[d];
        }

        // End guard
        modules += "101";

        return modules;
    }

    private static SKTypeface ResolveTypeface(string[] preferredFamilies)
    {
        foreach (var family in preferredFamilies)
        {
            var tf = SKTypeface.FromFamilyName(family);
            if (tf != null) return tf;
        }
        return SKTypeface.Default;
    }
}