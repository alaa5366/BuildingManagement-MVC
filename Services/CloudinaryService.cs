using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using BuildingManagementMvc.Models;   // ← ضيف ده


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
    public async Task<ReceiptData?> UploadReceiptAsync(Stream imageStream, string fileName, string buildingId, string aptId)
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

            return new ReceiptData
            {
                Url = result.SecureUrl.ToString(),
                Path = result.PublicId,
                FileName = fileName,
                FileSize = result.Bytes,
                FileType = result.Format,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cloudinary] Upload exception");
            return null;
        }
    }
}