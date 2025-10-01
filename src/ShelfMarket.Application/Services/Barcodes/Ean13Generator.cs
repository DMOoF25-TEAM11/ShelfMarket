using System.Globalization;
using System.Text;
using ShelfMarket.Application.Abstract.Services.Barcodes;
using SkiaSharp;

namespace ShelfMarket.Application.Services.Barcodes;

/// <inheritdoc />
public sealed class Ean13BarcodeGenerator : IEan13Generator
{
    #region Static Data Tables
    /// <summary>
    /// Left side (odd parity) encoding patterns for digits 0–9 (L-codes).
    /// </summary>
    private static readonly string[] L =
    [
        "0001101","0011001","0010011","0111101","0100011",
        "0110001","0101111","0111011","0110111","0001011"
    ];

    /// <summary>
    /// Left side (even parity) encoding patterns for digits 0–9 (G-codes).
    /// </summary>
    private static readonly string[] G =
    [
        "0100111","0110011","0011011","0100001","0011101",
        "0111001","0000101","0010001","0001001","0010111"
    ];

    /// <summary>
    /// Right side encoding patterns for digits 0–9 (R-codes).
    /// </summary>
    private static readonly string[] R =
    [
        "1110010","1100110","1101100","1000010","1011100",
        "1001110","1010000","1000100","1001000","1110100"
    ];

    /// <summary>
    /// Parity map indexed by the first (leading) digit. Each string of 6 chars
    /// determines whether the subsequent 6 left-side digits use L or G pattern.
    /// </summary>
    private static readonly string[] ParityByFirstDigit =
    [
        "LLLLLL","LLGLGG","LLGGLG","LLGGGL","LGLLGG",
        "LGGLLG","LGGGLL","LGLGLG","LGLGGL","LGGLGL",
    ];
    #endregion

    /// <summary>
    /// Danish culture used for price formatting in header text.
    /// </summary>
    private static readonly CultureInfo DanishCulture = new("da-DK");

    /// <summary>
    /// Preferred font fallback chain for rendering text.
    /// </summary>
    private static readonly string[] FontFamilies = ["Segoe UI", "Arial", "Liberation Sans", "DejaVu Sans", "Noto Sans", "Ubuntu"];

    /// <summary>
    /// Resolved normal (regular weight) typeface.
    /// </summary>
    private static readonly SKTypeface NormalTypeface = ResolveTypeface(FontFamilies);

    /// <summary>
    /// Resolved bold typeface (falls back to normal if unavailable).
    /// </summary>
    private static readonly SKTypeface BoldTypeface = ResolveBoldTypeface(FontFamilies, NormalTypeface);

    /// <inheritdoc />
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
            throw new ArgumentException($"UnitPrice (in cents) has more than {priceDigits} digits.", nameof(price));

        shelf = shelf.PadLeft(shelfDigits, '0');
        cents = cents.PadLeft(priceDigits, '0');

        return shelf + cents;
    }

    /// <inheritdoc />
    public static int ComputeCheckDigit(string data12)
    {
        if (data12 is null || data12.Length != 12)
            throw new ArgumentException("data12 must be exactly 12 digits.", nameof(data12));
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = data12[i] - '0';
            if (digit < 0 || digit > 9)
                throw new ArgumentException("data12 must be exactly 12 digits.", nameof(data12));
            // Even index (0-based) weight = 1, odd index weight = 3.
            sum += ((i & 1) == 1) ? digit * 3 : digit;
        }
        int mod = sum % 10;
        return (10 - mod) % 10;
    }

    /// <inheritdoc />
    public string Build(string shelfNumberDigits, decimal price, int shelfDigits = 6, int priceDigits = 6)
    {
        string data12 = ComposeData12(shelfNumberDigits, price, shelfDigits, priceDigits);
        int check = ComputeCheckDigit(data12);
        return data12 + check.ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public byte[] RenderPng(string ean13, int scale = 3, int barHeight = 60, bool includeNumbers = true)
    {
        ValidateEan13(ean13);
        if (scale < 1) throw new ArgumentOutOfRangeException(nameof(scale));
        if (barHeight < 10) throw new ArgumentOutOfRangeException(nameof(barHeight), "barHeight should be >= 10.");

        string modules = EncodeModules(ean13);
        const int shelfDigits = 6;
        const int priceDigits = 6;
        string shelfPart = ean13.Substring(0, shelfDigits);
        string pricePart = ean13.Substring(shelfDigits, priceDigits);
        if (!long.TryParse(pricePart, out var priceCents))
            throw new ArgumentException("Unable to parse price digits.", nameof(ean13));
        decimal price = priceCents / 100m;

        int quiet = 10;
        int totalModules = quiet + modules.Length + quiet;
        int width = totalModules * scale;

        float headerFontSize = 24f;
        float baseline, headerX = 4f;
        float headerPaddingTop = 4f, headerPaddingBottom = 6f;
        float lineHeight, ascent, descent;
        using SKFont fontNormal = new(NormalTypeface, headerFontSize);
        using SKFont fontBold = new(BoldTypeface, headerFontSize);
        fontNormal.GetFontMetrics(out var normalMetrics);
        fontBold.GetFontMetrics(out var boldMetrics);
        ascent = Math.Min(normalMetrics.Ascent, boldMetrics.Ascent);
        descent = Math.Max(normalMetrics.Descent, boldMetrics.Descent);
        lineHeight = descent - ascent;
        float headerAreaHeight = headerPaddingTop + lineHeight + headerPaddingBottom;

        float digitsFontSize = 12f;
        int digitsAreaHeight = 0;
        SKFont? digitsFont = null;
        SKFontMetrics digitsMetrics = default;
        if (includeNumbers)
        {
            digitsFont = new SKFont(NormalTypeface, digitsFontSize);
            digitsFont.GetFontMetrics(out digitsMetrics);
            float digitsLineHeight = digitsMetrics.Descent - digitsMetrics.Ascent;
            digitsAreaHeight = (int)Math.Ceiling(digitsLineHeight + 6);
        }
        int height = (int)Math.Ceiling(headerAreaHeight) + barHeight + digitsAreaHeight;
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        string shelfDisplayRaw = shelfPart.TrimStart('0');
        string shelfDisplay = string.IsNullOrEmpty(shelfDisplayRaw) ? "0" : shelfDisplayRaw;
        string priceDisplay = price.ToString("0.00", DanishCulture);
        string part1 = $"Reol: {shelfDisplay}  ";
        string part2 = $"Pris {priceDisplay} kr.";
        using var paintNormal = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var paintBold = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        baseline = headerPaddingTop - ascent;
        float part1Width = fontNormal.MeasureText(part1, paintNormal);
        canvas.DrawText(part1, headerX, baseline, SKTextAlign.Left, fontNormal, paintNormal);
        canvas.DrawText(part2, headerX + part1Width, baseline, SKTextAlign.Left, fontBold, paintBold);

        int barsTop = (int)Math.Ceiling(headerAreaHeight);
        using (var barPaint = new SKPaint { Color = SKColors.Black, IsAntialias = false, Style = SKPaintStyle.Fill })
        {
            int x = quiet * scale;
            int runStart = -1;
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == '1')
                {
                    if (runStart == -1) runStart = i;
                }
                else if (runStart != -1)
                {
                    var rect = new SKRect(x - (i - runStart) * scale, barsTop, x, barsTop + barHeight);
                    canvas.DrawRect(rect, barPaint);
                    runStart = -1;
                }
                x += scale;
            }
            if (runStart != -1)
            {
                var rect = new SKRect(x - (modules.Length - runStart) * scale, barsTop, x, barsTop + barHeight);
                canvas.DrawRect(rect, barPaint);
            }
        }

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


    /// <inheritdoc />
    public byte[] RenderPng(string ean13, double targetWidthMm, double targetHeightMm, int dpi = 300, bool includeNumbers = true)
    {
        if (targetWidthMm <= 0 || targetHeightMm <= 0) throw new ArgumentOutOfRangeException("Label dimensions must be > 0.");
        if (dpi < 72) throw new ArgumentOutOfRangeException(nameof(dpi));
        ValidateEan13(ean13);
        string modules = EncodeModules(ean13);
        int quiet = 10;
        int totalModules = quiet + modules.Length + quiet;
        double targetWidthPx = targetWidthMm / 25.4 * dpi;
        double targetHeightPx = targetHeightMm / 25.4 * dpi;
        int scale = (int)Math.Floor(targetWidthPx / totalModules);
        if (scale < 1) scale = 1;
        int barHeight = (int)Math.Round(targetHeightPx * 0.65);
        if (barHeight < 30) barHeight = 30;
        return RenderPng(ean13, scale, barHeight, includeNumbers);
    }

    /// <inheritdoc />
    public Task<byte[]> RenderPngAsync(string ean13, int scale = 3, int barHeight = 60, bool includeNumbers = true, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return RenderPng(ean13, scale, barHeight, includeNumbers);
        }, cancellationToken);
    }

    /// <summary>
    /// Validates a 13-digit EAN for format and check digit integrity.
    /// </summary>
    /// <param name="ean13">EAN string to validate.</param>
    /// <exception cref="ArgumentException">If the value is not 13 digits or the check digit mismatches.</exception>
    public void ValidateEan13(string ean13)
    {
        if (ean13 is null || ean13.Length != 13)
            throw new ArgumentException("ean13 must be exactly 13 digits.", nameof(ean13));
        for (int i = 0; i < 13; i++)
        {
            if (ean13[i] < '0' || ean13[i] > '9')
                throw new ArgumentException("ean13 must be exactly 13 digits.", nameof(ean13));
        }
        string data12 = ean13[..12];
        int expected = ComputeCheckDigit(data12);
        int actual = ean13[12] - '0';
        if (expected != actual)
            throw new ArgumentException("Invalid EAN-13: check digit mismatch.", nameof(ean13));
    }

    /// <summary>
    /// Removes all non-digit characters from a string.
    /// </summary>
    /// <param name="raw">Input string (nullable).</param>
    /// <returns>Digits-only string (may be empty).</returns>
    private static string NormalizeDigits(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (c >= '0' && c <= '9') sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts a price (decimal) into an absolute cents string using midpoint rounding away from zero.
    /// </summary>
    /// <param name="price">The monetary amount.</param>
    /// <returns>Unsigned cents string (no sign indicator).</returns>
    private static string ToCents(decimal price)
    {
        int cents = (int)Math.Round(price * 100m, MidpointRounding.AwayFromZero);
        return Math.Abs(cents).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Encodes the 13-digit EAN number into a 95-module binary string ("1" = black, "0" = white) including guards.
    /// </summary>
    /// <param name="ean13">Validated 13-digit EAN.</param>
    /// <returns>Binary pattern string of length 95.</returns>
    private static string EncodeModules(string ean13)
    {
        int first = ean13[0] - '0';
        string parity = ParityByFirstDigit[first];
        var sb = new StringBuilder(95);
        sb.Append("101");
        for (int i = 1; i <= 6; i++)
        {
            int d = ean13[i] - '0';
            bool useG = parity[i - 1] == 'G';
            sb.Append(useG ? G[d] : L[d]);
        }
        sb.Append("01010");
        for (int i = 7; i < 13; i++)
        {
            int d = ean13[i] - '0';
            sb.Append(R[d]);
        }
        sb.Append("101");
        return sb.ToString();
    }

    /// <summary>
    /// Attempts to resolve a preferred typeface by iterating candidate family names, falling back to default.
    /// </summary>
    /// <param name="preferredFamilies">Ordered list of font family names.</param>
    /// <returns>An <see cref="SKTypeface"/> instance (never null).</returns>
    private static SKTypeface ResolveTypeface(string[] preferredFamilies)
    {
        foreach (var family in preferredFamilies)
        {
            var tf = SKTypeface.FromFamilyName(family);
            if (tf != null) return tf;
        }
        return SKTypeface.Default;
    }

    /// <summary>
    /// Attempts to resolve a bold variant from preferred families; falls back to provided regular typeface.
    /// </summary>
    /// <param name="preferredFamilies">Ordered font families.</param>
    /// <param name="fallback">Fallback typeface if no bold variant found.</param>
    /// <returns>A bold typeface or the fallback.</returns>
    private static SKTypeface ResolveBoldTypeface(string[] preferredFamilies, SKTypeface fallback)
    {
        foreach (var f in preferredFamilies)
        {
            var candidate = SKTypeface.FromFamilyName(f, SKFontStyle.Bold);
            if (candidate != null)
                return candidate;
        }
        return fallback;
    }

    /// <inheritdoc />
    int IEan13Generator.ComputeCheckDigit(string data12) => ComputeCheckDigit(data12);
    void IEan13Generator.ValidateEan13(string ean) => ValidateEan13(ean);
}