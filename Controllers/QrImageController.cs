using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  QrImageController	توليد صور QR
// ============================================================
/// <summary>
/// Generates QR code images locally so login and set-PIN URLs (which contain tokens) never leave the server.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin,superadmin</c>.
/// </remarks>
[Authorize(Roles = "admin,superadmin")]
public class QrImageController : Controller
{
    private readonly QrGeneratorService _qr;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrImageController"/> class.
    /// </summary>
    public QrImageController(QrGeneratorService qr) => _qr = qr;

    /// <summary>
    /// Returns a PNG image of a QR code for an HTTP or HTTPS URL. Admin and super admin only.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public IActionResult Png(string data, int size = 280)
    {
        if (string.IsNullOrWhiteSpace(data) || data.Length > 1500)
            return BadRequest();

        if (!data.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !data.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return BadRequest();

        var pixelsPerModule = Math.Clamp(size / 35, 3, 14);
        var png = _qr.GeneratePng(data, pixelsPerModule);

        // الرابط ممكن يحتوي توكن — مايتخزّنش في كاش مشترك
        Response.Headers["Cache-Control"] = "private, no-store";
        return File(png, "image/png");
    }
}
