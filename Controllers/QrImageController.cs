using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

// توليد صور QR محليًا. قبل كده الصفحات كانت بتبعت الرابط (فيه توكنات الدخول وتعيين الـ PIN)
// لموقع خارجي (api.qrserver.com) عشان يرسم الصورة — ودلوقتي الرابط مابيخرجش من السيرفر.
[Authorize(Roles = "admin,superadmin")]
public class QrImageController : Controller
{
    private readonly QrGeneratorService _qr;

    public QrImageController(QrGeneratorService qr) => _qr = qr;

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
