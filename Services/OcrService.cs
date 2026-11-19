using Tesseract;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Extracts text from a receipt image with Tesseract.
/// </summary>
public class OcrService
{
    private readonly string _tessDataPath;
    private readonly ILogger<OcrService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrService"/> class.
    /// </summary>
    public OcrService(IWebHostEnvironment env, ILogger<OcrService> logger)
    {
        _tessDataPath = Path.Combine(env.WebRootPath, "tessdata");
        _logger = logger;
    }

    /// <summary>
    /// Returns the extract text.
    /// </summary>
    public string ExtractText(Stream imageStream)
    {
        try
        {
            var imageBytes = ReadFully(imageStream);

            using var engine = new TesseractEngine(_tessDataPath, "eng+ara", EngineMode.Default);
            engine.SetVariable("preserve_interword_spaces", "1");

            using var img = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(img, PageSegMode.Auto);

            // النص المستخرج
            var text = page.GetText();
            return text ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OCR] Failed");
            return "";
        }
    }

    private static byte[] ReadFully(Stream input)
    {
        if (input is MemoryStream ms) return ms.ToArray();
        using var memoryStream = new MemoryStream();
        input.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
