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

        // Quiet zones (10 modules each side)
        int quiet = 10;
        int totalModules = quiet + modules.Length + quiet;
        int width = totalModules * scale;

        // Header (single line) configuration
        float headerFontSize = 24f;
        var families = new[] { "Segoe UI", "Arial", "Liberation Sans", "DejaVu Sans", "Noto Sans", "Ubuntu" };
        var normalTypeface = ResolveTypeface(families);

        // Try to find a bold variant
        SKTypeface boldTypeface = normalTypeface;
        foreach (var f in families)
        {
            var candidate = SKTypeface.FromFamilyName(f, SKFontStyle.Bold);
            if (candidate != null)
            {
                boldTypeface = candidate;
                break;
            }
        }

        using SKFont fontNormal = new(normalTypeface, headerFontSize);
        using SKFont fontBold = new(boldTypeface, headerFontSize);

        fontNormal.GetFontMetrics(out var normalMetrics);
        fontBold.GetFontMetrics(out var boldMetrics);

        // Compute composite line metrics
        float ascent = Math.Min(normalMetrics.Ascent, boldMetrics.Ascent);   // Ascent values are negative
        float descent = Math.Max(normalMetrics.Descent, boldMetrics.Descent);
        float lineHeight = descent - ascent;
        float headerPaddingTop = 4f;
        float headerPaddingBottom = 6f;
        float headerAreaHeight = headerPaddingTop + lineHeight + headerPaddingBottom;

        // Digits line (optional)
        float digitsFontSize = 12f;
        int digitsAreaHeight = 0;
        SKFont? digitsFont = null;
        SKFontMetrics digitsMetrics = default;

        if (includeNumbers)
        {
            digitsFont = new SKFont(normalTypeface, digitsFontSize);
            digitsFont.GetFontMetrics(out digitsMetrics);
            float digitsLineHeight = digitsMetrics.Descent - digitsMetrics.Ascent;
            digitsAreaHeight = (int)Math.Ceiling(digitsLineHeight + 6);
        }

        int height = (int)Math.Ceiling(headerAreaHeight) + barHeight + digitsAreaHeight;

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        // Prepare header text parts
        var culture = new CultureInfo("da-DK");
        string shelfDisplayRaw = shelfPart.TrimStart('0');
        string shelfDisplay = string.IsNullOrEmpty(shelfDisplayRaw) ? "0" : shelfDisplayRaw;
        string priceDisplay = price.ToString("0.00", culture);

        string part1 = $"Reol: {shelfDisplay}  ";
        string part2 = $"Pris {priceDisplay} kr."; // 'pris' (whole phrase) bold as requested

        using var paintNormal = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var paintBold = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        float baseline = headerPaddingTop - ascent; // ascent is negative
        float headerX = 4f;

        // Draw first (normal) part
        float part1Width = fontNormal.MeasureText(part1, paintNormal);
        canvas.DrawText(part1, headerX, baseline, SKTextAlign.Left, fontNormal, paintNormal);

        // Draw second (bold) part
        canvas.DrawText(part2, headerX + part1Width, baseline, SKTextAlign.Left, fontBold, paintBold);

        // Bars start after header
        int barsTop = (int)Math.Ceiling(headerAreaHeight);
        using (var barPaint = new SKPaint { Color = SKColors.Black, IsAntialias = false, Style = SKPaintStyle.Fill })
        {
            int x = quiet * scale;
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == '1')
                {
                    var rect = new SKRect(x, barsTop, x + scale, barsTop + barHeight);
                    canvas.DrawRect(rect, barPaint);
                }
                x += scale;
            }
        }

        // Optional EAN digits below
        if (includeNumbers && digitsFont != null)
        {
            using var digitsPaint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            float digitsBaseline = barsTop + barHeight + 2 - digitsMetrics.Ascent;
            float textWidth = digitsFont.MeasureText(ean13, digitsPaint);
            float tx = (width - textWidth) / 2f;
            canvas.DrawText(ean13, tx, digitsBaseline, SKTextAlign.Left, digitsFont, digitsPaint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        digitsFont?.Dispose();
        return data.ToArray();
    }

    // New overload to render scaled to a target physical label size
    public byte[] RenderPng(string ean13,
                            double targetWidthMm,
                            double targetHeightMm,
                            int dpi = 300,
                            bool includeNumbers = true)
    {
        if (targetWidthMm <= 0 || targetHeightMm <= 0) throw new ArgumentOutOfRangeException("Label dimensions must be > 0.");
        if (dpi < 72) throw new ArgumentOutOfRangeException(nameof(dpi));
        ValidateEan13(ean13);

        string modules = EncodeModules(ean13);
        int quiet = 10;
        int totalModules = quiet + modules.Length + quiet;

        // Convert mm to pixels
        double targetWidthPx = targetWidthMm / 25.4 * dpi;
        double targetHeightPx = targetHeightMm / 25.4 * dpi;

        // Choose module scale so barcode fits width
        int scale = (int)Math.Floor(targetWidthPx / totalModules);
        if (scale < 1) scale = 1;

        // Reserve ~65% of height to bars (heuristic)
        int barHeight = (int)Math.Round(targetHeightPx * 0.65);
        if (barHeight < 30) barHeight = 30;

        // Render with existing logic (falls back to previous method)
        var png = RenderPng(ean13, scale, barHeight, includeNumbers);

        // If final width is smaller than target, that's acceptable; exact stretching done at print time if needed
        return png;
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