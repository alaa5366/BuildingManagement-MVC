using SkiaSharp;
using Tesseract;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Extracts bilingual receipt text using multiple image variants and Tesseract layout passes.
/// OCR output is advisory and must never be treated as payment confirmation.
/// </summary>
public class OcrService
{
    private const long MaximumPixels = 30_000_000;
    private readonly string _tessDataPath;
    private readonly ILogger<OcrService> _logger;

    public OcrService(IWebHostEnvironment env, ILogger<OcrService> logger)
    {
        _tessDataPath = Path.Combine(env.ContentRootPath, "App_Data", "tessdata");
        _logger = logger;
    }

    /// <summary>
    /// Reads English and Arabic receipt text from the supplied image stream. The stream is
    /// rewound when seekable, and several Leptonica variants are evaluated for difficult captures.
    /// </summary>
    public string ExtractText(Stream imageStream)
    {
        try
        {
            var imageBytes = ReadFully(imageStream);
            if (imageBytes.Length == 0)
                return string.Empty;

            // Skia normalizes WebP and other accepted upload formats to PNG because
            // the bundled Leptonica build does not decode every modern image format.
            imageBytes = NormalizeImageForTesseract(imageBytes);

            var englishModel = Path.Combine(_tessDataPath, "eng.traineddata");
            var arabicModel = Path.Combine(_tessDataPath, "ara.traineddata");
            if (!File.Exists(englishModel) || !File.Exists(arabicModel))
            {
                _logger.LogError(
                    "[OCR] Required Tesseract language models are missing. EnglishModelPresent={EnglishPresent}, ArabicModelPresent={ArabicPresent}",
                    File.Exists(englishModel), File.Exists(arabicModel));
                return string.Empty;
            }

            var fastDataPath = Path.Combine(_tessDataPath, "fast");
            var hasFastModels = File.Exists(Path.Combine(fastDataPath, "eng.traineddata")) &&
                                File.Exists(Path.Combine(fastDataPath, "ara.traineddata"));
            var baselineDataPath = hasFastModels ? fastDataPath : _tessDataPath;

            using var baselineEngine = new TesseractEngine(baselineDataPath, "eng+ara", EngineMode.LstmOnly);
            ConfigureEngine(baselineEngine);

            using var source = Pix.LoadFromMemory(imageBytes);
            if (source.Width <= 0 || source.Height <= 0 ||
                (long)source.Width * source.Height > MaximumPixels)
            {
                _logger.LogWarning(
                    "[OCR] Receipt image dimensions are invalid or exceed the processing limit. Width={Width}, Height={Height}",
                    source.Width, source.Height);
                return string.Empty;
            }

            var ownedVariants = new List<Pix>();
            try
            {
                var baselineCandidates = new List<(Pix Image, string Variant, PageSegMode[] Modes)>
                {
                    (source, "original", new[]
                    {
                        PageSegMode.Auto,
                        PageSegMode.SingleBlock
                    })
                };
                var baseline = EvaluateCandidates(baselineEngine, baselineCandidates, _logger);
                if (IsGoodEnough(baseline))
                    return baseline.Text;

                // Expensive preprocessing and orientation detection are reserved for
                // low-confidence captures so normal receipts do not pay the full OCR cost.
                var candidates = new List<(Pix Image, string Variant, PageSegMode[] Modes)>();
                var orientationModel = Path.Combine(_tessDataPath, "osd.traineddata");
                if (File.Exists(orientationModel) && baseline.Confidence < 0.65f)
                {
                    candidates.Add((source, "orientation-aware", new[] { PageSegMode.AutoOsd }));
                }

                Pix? gray = null;
                Pix? deskewed = null;
                Pix? scaled = null;

                try
                {
                    gray = source.ConvertRGBToGray();
                    ownedVariants.Add(gray);
                    candidates.Add((gray, "grayscale", new[]
                    {
                        PageSegMode.Auto,
                        PageSegMode.SingleBlock
                    }));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[OCR] Grayscale preprocessing was unavailable for this image");
                }

                if (gray is not null)
                {
                    try
                    {
                        deskewed = gray.Deskew();
                        if (!ReferenceEquals(deskewed, gray))
                        {
                            ownedVariants.Add(deskewed);
                            candidates.Add((deskewed, "deskewed", new[] { PageSegMode.SingleBlock }));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "[OCR] Deskew preprocessing was unavailable for this image");
                    }
                }

                var scaleSource = deskewed ?? gray;
                if (scaleSource is not null)
                {
                    try
                    {
                        var longestSide = Math.Max(scaleSource.Width, scaleSource.Height);
                        var scaleFactor = longestSide < 1400 ? 1.6f
                            : longestSide < 2100 ? 1.25f
                            : 1.0f;
                        var scaledPixels = (long)(scaleSource.Width * scaleFactor) *
                                           (long)(scaleSource.Height * scaleFactor);
                        if (scaleFactor > 1f && scaledPixels <= MaximumPixels)
                        {
                            scaled = scaleSource.Scale(scaleFactor, scaleFactor);
                            ownedVariants.Add(scaled);
                            candidates.Add((scaled, "upscaled", new[] { PageSegMode.SingleBlock }));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "[OCR] Upscaling preprocessing was unavailable for this image");
                    }
                }

                var thresholdSource = scaled ?? scaleSource;
                if (thresholdSource is not null)
                {
                    try
                    {
                        var sauvola = thresholdSource.BinarizeSauvola(31, 0.34f, true);
                        ownedVariants.Add(sauvola);
                        candidates.Add((sauvola, "sauvola", new[] { PageSegMode.SingleBlock }));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "[OCR] Sauvola thresholding was unavailable for this image");
                    }

                    try
                    {
                        var otsu = thresholdSource.BinarizeOtsuAdaptiveThreshold(32, 32, 8, 8, 0.1f);
                        ownedVariants.Add(otsu);
                        candidates.Add((otsu, "adaptive-otsu", new[] { PageSegMode.SingleBlock }));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "[OCR] Adaptive Otsu thresholding was unavailable for this image");
                    }
                }

                var advanced = EvaluateCandidates(baselineEngine, candidates, _logger);
                var selected = advanced.Score > baseline.Score ? advanced : baseline;

                // The slower accuracy model is a single refinement pass on the best
                // fast-model image/layout candidate, not a full second multi-pass sweep.
                if (hasFastModels && selected.Image is not null && !IsGoodEnough(selected))
                {
                    using var bestEngine = new TesseractEngine(_tessDataPath, "eng+ara", EngineMode.LstmOnly);
                    ConfigureEngine(bestEngine);
                    var refinement = EvaluateCandidates(
                        bestEngine,
                        new[] { (selected.Image, "best-model-refinement", new[] { selected.Mode }) },
                        _logger);
                    if (refinement.Score > selected.Score)
                        selected = refinement;
                }

                return selected.Text;
            }
            finally
            {
                for (var index = ownedVariants.Count - 1; index >= 0; index--)
                    ownedVariants[index].Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OCR] Receipt extraction failed");
            return string.Empty;
        }
    }

    private static void ConfigureEngine(TesseractEngine engine)
    {
        engine.SetVariable("preserve_interword_spaces", "1");
        engine.SetVariable("user_defined_dpi", "300");
    }

    private static bool IsGoodEnough((string Text, double Score, float Confidence, string Variant, PageSegMode Mode, Pix? Image) result)
    {
        if (result.Confidence < 0.78f || result.Text.Length < 60)
            return false;

        var hasAmount = System.Text.RegularExpressions.Regex.IsMatch(
            result.Text,
            @"amount|total|المبلغ|قيمة التحويل|إجمالي|الاجمالي|جنيه|EGP|LE",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        var hasReferenceOrStatus = System.Text.RegularExpressions.Regex.IsMatch(
            result.Text,
            @"reference|transaction|transfer|رقم العملية|رقم التحويل|مرجع|رقم المعاملة|successful|success|completed|accepted|failed|declined|rejected|pending|بنجاح|نجاح|مقبول|فشل|مرفوض|قيد التنفيذ|معلق",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        return result.Score >= 0.68d && hasAmount && hasReferenceOrStatus;
    }

    private static (string Text, double Score, float Confidence, string Variant, PageSegMode Mode, Pix? Image) EvaluateCandidates(
        TesseractEngine engine,
        IReadOnlyList<(Pix Image, string Variant, PageSegMode[] Modes)> candidates,
        ILogger logger)
    {
        string bestText = string.Empty;
        string bestVariant = "none";
        PageSegMode bestMode = PageSegMode.Auto;
        var bestScore = double.NegativeInfinity;
        var bestConfidence = 0f;
        Pix? bestImage = null;

        foreach (var candidate in candidates)
        {
            foreach (var mode in candidate.Modes)
            {
                try
                {
                    using var page = engine.Process(candidate.Image, mode);
                    var text = page.GetText() ?? string.Empty;
                    var confidence = page.GetMeanConfidence();
                    var score = ScoreCandidate(text, confidence);
                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    bestText = text;
                    bestVariant = candidate.Variant;
                    bestMode = mode;
                    bestConfidence = confidence;
                    bestImage = candidate.Image;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(
                        ex,
                        "[OCR] Receipt OCR pass failed. Variant={Variant}, PageSegmentationMode={Mode}",
                        candidate.Variant,
                        mode);
                }
            }
        }

        logger.LogInformation(
            "[OCR] Receipt extraction completed. Variant={Variant}, PageSegmentationMode={Mode}, MeanConfidence={MeanConfidence:F3}, CandidateScore={CandidateScore:F3}, SelectedTextLength={TextLength}",
            bestVariant,
            bestMode,
            bestConfidence,
            bestScore,
            bestText.Length);

        return (bestText, bestScore, bestConfidence, bestVariant, bestMode, bestImage);
    }

    private static double ScoreCandidate(string text, float confidence)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0d;

        var score = Math.Clamp(confidence, 0f, 1f) * 0.65d;
        score += Math.Min(text.Length / 500d, 1d) * 0.08d;

        if (System.Text.RegularExpressions.Regex.IsMatch(
            text,
            @"amount|total|المبلغ|قيمة التحويل|إجمالي|الاجمالي|جنيه|EGP|LE",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            score += 0.10d;

        if (System.Text.RegularExpressions.Regex.IsMatch(
            text,
            @"reference|transaction|transfer|رقم العملية|رقم التحويل|مرجع|رقم المعاملة",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            score += 0.07d;

        if (System.Text.RegularExpressions.Regex.IsMatch(
            text,
            @"successful|success|completed|accepted|failed|declined|rejected|pending|بنجاح|نجاح|مقبول|فشل|مرفوض|قيد التنفيذ|معلق",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.CultureInvariant))
            score += 0.10d;

        return score;
    }

    private static byte[] NormalizeImageForTesseract(byte[] imageBytes)
    {
        using var encodedData = SKData.CreateCopy(imageBytes);
        using var codec = SKCodec.Create(encodedData)
            ?? throw new InvalidDataException("The uploaded image format is not supported.");

        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 ||
            (long)info.Width * info.Height > MaximumPixels)
        {
            throw new InvalidDataException("The uploaded image dimensions exceed the OCR processing limit.");
        }

        using var bitmap = SKBitmap.Decode(codec)
            ?? throw new InvalidDataException("The uploaded image could not be decoded.");

        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidDataException("The uploaded image could not be normalized for OCR.");

        return png.ToArray();
    }

    private static byte[] ReadFully(Stream input)
    {
        if (input.CanSeek)
            input.Position = 0;

        using var memoryStream = new MemoryStream();
        input.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
