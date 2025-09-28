using ShelfMarket.Application.Services.Barcodes;

namespace ShelfMarket.Application.Tests;

[TestClass]
public class Ean13BarcodeGeneratorTests
{
    private readonly Ean13BarcodeGenerator gen = new();

    [TestMethod]
    public void ComposeData12_PadsShelvesAndPrice()
    {
        var result = gen.ComposeData12("A12-3", 4.56m, 6, 6);
        Assert.AreEqual("000123000456", result);
    }

    [TestMethod]
    public void ComputeCheckDigit_KnownExample()
    {
        // Standard example: 5901234123457 -> check digit 7
        var check = Ean13BarcodeGenerator.ComputeCheckDigit("590123412345");
        Assert.AreEqual(7, check);
    }

    [TestMethod]
    public void BuildEan13_UsesComputedCheckDigit()
    {
        string data12 = gen.ComposeData12("123", 4.56m, 6, 6);
        int check = Ean13BarcodeGenerator.ComputeCheckDigit(data12);
        string ean = gen.Build("123", 4.56m, 6, 6);

        Assert.AreEqual(data12 + check.ToString(), ean);
        Assert.AreEqual(13, ean.Length);
    }

    [TestMethod]
    public void RenderPng_ReturnsPngBytes()
    {
        string ean = gen.Build("123", 4.56m, 6, 6);
        var png = gen.RenderPng(ean, scale: 2, barHeight: 40, includeNumbers: true);

        Assert.IsNotNull(png);
        Assert.IsTrue(png.Length > 100);
        byte[] sig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        CollectionAssert.AreEqual(sig, png.Take(8).ToArray());
    }

    [TestMethod]
    public void RenderPng_InvalidCheckDigit_Throws()
    {
        string valid = gen.Build("123", 4.56m, 6, 6);
        char wrong = (char)(((valid[^1] - '0' + 1) % 10) + '0');
        string invalid = valid[..12] + wrong;

        Assert.ThrowsExactly<ArgumentException>(() => gen.RenderPng(invalid));
    }

    [TestMethod]
    public async Task RenderPngAsync_Canceled_Throws()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await gen.RenderPngAsync("0000000000000", cancellationToken: cts.Token);
        });
    }

    [TestMethod]
    public void ComposeData12_InvalidDigitConfig_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => gen.ComposeData12("1", 0m, 5, 5));
    }

    [TestMethod]
    public void ComputeCheckDigit_InvalidData12_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Ean13BarcodeGenerator.ComputeCheckDigit("123"));
    }
}