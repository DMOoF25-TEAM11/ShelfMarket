namespace ShelfMarket.Application.Abstract.Services.Barcodes
{
    public interface IEan13Generator
    {
        // Compose the 12 data digits by concatenating shelfNumber and price (in cents) with left-zero padding.
        // Defaults: shelf=6 digits, price=6 digits => 12 digits total.
        string ComposeData12(string shelfNumberDigits, decimal price, int shelfDigits = 6, int priceDigits = 6);

        // Compute the EAN-13 check digit for the provided 12-digit data string.
        int ComputeCheckDigit(string data12);

        // Returns the final 13-digit EAN string using the provided parts.
        string Build(string shelfNumberDigits, decimal price, int shelfDigits = 6, int priceDigits = 6);

        // Render the EAN-13 barcode to a PNG byte array.
        // scale: pixels per module; barHeight: height of bars (px); includeNumbers: draw EAN digits underneath.
        byte[] RenderPng(string ean13, int scale = 3, int barHeight = 60, bool includeNumbers = true);

        // Async version of PNG rendering (off-UI-thread).
        Task<byte[]> RenderPngAsync(string ean13, int scale = 3, int barHeight = 60, bool includeNumbers = true, CancellationToken cancellationToken = default);
    }
}