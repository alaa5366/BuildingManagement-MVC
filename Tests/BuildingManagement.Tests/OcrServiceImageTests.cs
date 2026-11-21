using System.Text.RegularExpressions;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class OcrServiceImageTests
{
    [Theory]
    [InlineData("instapay-success-en.png", @"successful|success|completed", @"EGP|1,250\.75", "IP-20261121-0001")]
    [InlineData("wallet-failed-ar.png", @"failed|rejected|\u0645\u0631\u0641\u0648\u0636|\u0641\u0634\u0644", @"EGP|LE|\u062c\u0646\u064a\u0647|\u0627\u0644\u0645\u0628\u0644\u063a", "WLT-20261121-0003")]
    [InlineData("wallet-pending-en.png", @"pending", @"EGP|875\.00", "WLT-20261121-0004")]
    [InlineData("bank-transfer-success-en.png", @"successful|completed", @"EGP|12,500\.75", "BNK-20261121-0007")]
    [InlineData("bank-transfer-failed-ar.png", @"failed|rejected|\u0645\u0631\u0641\u0648\u0636|\u0631\u0641\u0636", @"EGP|LE|\u062c\u0646\u064a\u0647|\u0627\u0644\u0645\u0628\u0644\u063a", "BNK-20261121-0008")]
    public void ExtractText_RecognizesSyntheticReceiptStateAmountAndReference(
        string fileName,
        string statePattern,
        string amountPattern,
        string reference)
    {
        var service = CreateService();
        using var stream = File.OpenRead(GetFixturePath(fileName));

        var text = service.ExtractText(stream);

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Matches(new Regex(statePattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), text);
        Assert.Matches(new Regex(amountPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), text);
        Assert.Contains(reference, text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractText_NormalizesWebpBeforeTesseractProcessing()
    {
        var png = File.ReadAllBytes(GetFixturePath("instapay-success-en.png"));
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);

        using var image = SKImage.FromBitmap(bitmap!);
        using var webp = image.Encode(SKEncodedImageFormat.Webp, 88);
        Assert.NotNull(webp);

        var service = CreateService();
        using var stream = new MemoryStream(webp!.ToArray());

        var text = service.ExtractText(stream);

        Assert.Matches(new Regex(@"successful|success|completed", RegexOptions.IgnoreCase), text);
        Assert.Matches(new Regex(@"EGP|1,250\.75", RegexOptions.IgnoreCase), text);
    }

    [Fact]
    public void ExtractText_RecognizesQuarterTurnedReceiptImage()
    {
        var png = File.ReadAllBytes(GetFixturePath("instapay-success-en.png"));
        using var bitmap = SKBitmap.Decode(png);
        Assert.NotNull(bitmap);

        using var rotated = new SKBitmap(bitmap!.Height, bitmap.Width);
        using (var canvas = new SKCanvas(rotated))
        {
            canvas.Clear(SKColors.White);
            canvas.Translate(rotated.Width, 0);
            canvas.RotateDegrees(90);
            canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
            canvas.Flush();
        }

        using var image = SKImage.FromBitmap(rotated);
        using var rotatedPng = image.Encode(SKEncodedImageFormat.Png, 100);
        Assert.NotNull(rotatedPng);

        var service = CreateService();
        using var stream = new MemoryStream(rotatedPng!.ToArray());

        var text = service.ExtractText(stream);

        Assert.Matches(new Regex(@"successful|success|completed", RegexOptions.IgnoreCase), text);
        Assert.Matches(new Regex(@"EGP|1,250\.75", RegexOptions.IgnoreCase), text);
    }

    [Fact]
    public void ExtractText_ReturnsEmptyForInvalidImageBytes()
    {
        var service = CreateService();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.Equal(string.Empty, service.ExtractText(stream));
    }

    private static OcrService CreateService()
    {
        var root = FindRepositoryRoot();
        return new OcrService(new TestEnvironment(root), NullLogger<OcrService>.Instance);
    }

    private static string GetFixturePath(string fileName) =>
        Path.Combine(
            FindRepositoryRoot(),
            "Tests",
            "BuildingManagement.Tests",
            "TestData",
            "Receipts",
            fileName);

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null &&
               !File.Exists(Path.Combine(current.FullName, "BuildingManagementMVC.csproj")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the application content root.");
    }

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "BuildingManagement.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
