using BuildingManagementMvc.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace BuildingManagementMvc.Services;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
        _logger = logger;

        var cloudName = config["Cloudinary:CloudName"];
        var apiKey = config["Cloudinary:ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            _logger.LogWarning("[Cloudinary] Configuration missing");
            _cloudinary = null!;
            return;
        }

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    /// <summary>
    /// يرفع صورة إيصال ويـرجع بيانات الصورة (URL، المسار، الحجم، النوع).
    /// </summary>
    public async Task<ReceiptData?> UploadReceiptAsync(
        Stream imageStream, string fileName, string buildingId, string aptId)
    {
        if (_cloudinary == null)
        {
            _logger.LogError("[Cloudinary] Not configured");
            return null;
        }

        try
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, imageStream),
                Folder = $"buildings/{buildingId}/receipts",
                PublicId = $"receipt-{aptId}-{Guid.NewGuid():N}",
                Overwrite = false,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                _logger.LogError($"[Cloudinary] Upload failed: {result.Error.Message}");
                return null;
            }

            // ✅ Normalize file type: "image/jpeg" → "jpeg", "jpg" → "jpg"
            var normalizedType = NormalizeFileType(result.Format);

            return new ReceiptData
            {
                Url = result.SecureUrl.ToString(),
                Path = result.PublicId,
                FileName = fileName,        // ← الاسم الأصلي من IFormFile
                FileSize = result.Bytes,
                FileType = normalizedType,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cloudinary] Upload exception");
            return null;
        }
    }

    // ============================================================
    // Helper: توحيد صيغة نوع الملف
    // ============================================================
    private static string NormalizeFileType(string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return "";

        var f = format.Trim().ToLowerInvariant();

        // شيل "image/"
        if (f.StartsWith("image/")) f = f[6..];

        // توحيد jpeg
        return f switch
        {
            "jpg" or "jpeg" => "jpg",
            _ => f
        };
    }
}