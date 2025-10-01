namespace ShelfMarket.Application.Abstract.Services.Barcodes;

/// <summary>
/// Generates EAN-13 barcode data (including check digit) and renders barcodes as PNG images using SkiaSharp.
/// </summary>
/// <remarks>
/// Responsibilities:
/// 1. Compose the first 12 data digits from a shelf identifier and a price (in cents).
/// 2. Compute the EAN-13 check digit (mod 10 with weighting 1 / 3).
/// 3. Encode digits into left (L/G) and right (R) patterns according to the standard parity table.
/// 4. Render barcode with optional human-readable header (shelf + price) and optional numeric digits beneath.
/// 5. Provide synchronous and asynchronous render methods and size-by-millimeters overload.
/// 
/// Implementation Notes:
/// - The encoding uses pre-defined bit patterns for L, G, and R sets.
/// - Guard patterns: start (101), middle (01010), end (101).
/// - The module sequence length is always 95 for EAN‑13 (including guards).
/// - Rendering uses run-length consolidation for black modules to minimize draw calls.
/// - Typeface resolution falls back through a list of common sans-serif families before using <see cref="SKTypeface.Default"/>.
/// - Culture-specific price formatting uses Danish locale (e.g. "12,50") for header text.
/// </remarks>
public interface IEan13Generator
{
    /// <summary>
    /// Composes the 12 data digits (without check digit) by concatenating a shelf number
    /// and price (converted to cents) with left zero-padding to fit <paramref name="shelfDigits"/>
    /// and <paramref name="priceDigits"/>.
    /// </summary>
    /// <param name="shelfNumberDigits">Raw shelf number string (non-digits ignored).</param>
    /// <param name="price">UnitPrice as decimal value; rounded to nearest cent (away from zero).</param>
    /// <param name="shelfDigits">Total digits allocated to shelf segment (must be &gt;= 1).</param>
    /// <param name="priceDigits">Total digits allocated to price (in cents) segment (must be &gt;= 1).</param>
    /// <returns>A 12-character numeric string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If digit counts are invalid.</exception>
    /// <exception cref="ArgumentException">If combined digit length ≠ 12 or inputs exceed allocated width.</exception>
    string ComposeData12(string shelfNumberDigits, decimal price, int shelfDigits = 6, int priceDigits = 6);

    /// <summary>
    /// Computes the EAN-13 check digit for a 12-digit data string.
    /// </summary>
    /// <param name="data12">Exactly 12 numeric digits.</param>
    /// <returns>The check digit (0–9).</returns>
    /// <exception cref="ArgumentException">If input is null, wrong length, or contains non-digits.</exception>
    int ComputeCheckDigit(string data12);

    /// <summary>
    /// Builds the full 13-digit EAN by composing data and appending the computed check digit.
    /// </summary>
    /// <param name="shelfNumberDigits">Shelf number string (non-digits removed).</param>
    /// <param name="price">UnitPrice value (converted to cents).</param>
    /// <param name="shelfDigits">Digits allotted to shelf part.</param>
    /// <param name="priceDigits">Digits allotted to price (cents) part.</param>
    /// <returns>The 13-digit EAN code.</returns>
    string Build(string shelfNumberDigits, decimal price, int shelfDigits = 6, int priceDigits = 6);

    /// <summary>
    /// Renders a PNG image of an EAN-13 barcode using pixel dimensions.
    /// </summary>
    /// <param name="ean13">Exactly 13-digit EAN with valid check digit.</param>
    /// <param name="scale">Pixels per module (module = 1 narrow bar width). Minimum 1.</param>
    /// <param name="barHeight">Height in pixels of the bar region (excluding text areas).</param>
    /// <param name="includeNumbers">Whether to draw the numeric digits beneath the bars.</param>
    /// <returns>PNG binary data.</returns>
    /// <exception cref="ArgumentException">If EAN invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If scale or barHeight are out of range.</exception>
    byte[] RenderPng(string ean13, int scale = 3, int barHeight = 60, bool includeNumbers = true);

    /// <summary>
    /// Renders a PNG image of an EAN-13 barcode sized to approximate target physical dimensions.
    /// </summary>
    /// <param name="ean13">13-digit EAN code.</param>
    /// <param name="targetWidthMm">Desired total image width in millimeters.</param>
    /// <param name="targetHeightMm">Desired total image height in millimeters.</param>
    /// <param name="dpi">Rendering DPI (must be ≥ 72).</param>
    /// <param name="includeNumbers">Whether to draw numeric digits.</param>
    /// <returns>PNG binary data.</returns>
    /// <remarks>
    /// The method chooses the largest integer scale (pixels per module) fitting within width constraint,
    /// then approximates bar height as 65% of available height (enforcing a minimum).
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">If width/height or dpi are invalid.</exception>
    /// <exception cref="ArgumentException">If EAN invalid.</exception>
    byte[] RenderPng(string ean13, double targetWidthMm, double targetHeightMm, int dpi = 300, bool includeNumbers = true);


    /// <summary>
    /// Asynchronous wrapper for <see cref="RenderPng(string,int,int,bool)"/> that executes off the UI thread.
    /// </summary>
    /// <param name="ean13">13-digit EAN code.</param>
    /// <param name="scale">Pixels per module.</param>
    /// <param name="barHeight">Bar height in pixels.</param>
    /// <param name="includeNumbers">Include human-readable digits.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task producing PNG binary data.</returns>
    Task<byte[]> RenderPngAsync(string ean13, int scale = 3, int barHeight = 60, bool includeNumbers = true, CancellationToken cancellationToken = default);
    void ValidateEan13(string ean13);

}