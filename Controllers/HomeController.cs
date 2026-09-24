using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("LoginChoice", "Account");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }

    [HttpGet]
    public IActionResult OcrTest()
    {
        var html = @"
            <!DOCTYPE html>
            <html>
            <head><meta charset='UTF-8'><title>اختبار OCR</title></head>
            <body style='font-family: Arial; padding: 20px;'>
                <h2>اختبار OCR — ارفع صورة التحويل</h2>
                <form method='post' enctype='multipart/form-data'>
                    <input type='file' name='image' accept='image/*' />
                    <button type='submit'>ارفع الصورة</button>
                </form>
            </body>
            </html>
        ";
        return Content(html, "text/html", System.Text.Encoding.UTF8);
    }

    [HttpPost]
    public async Task<IActionResult> OcrTest(IFormFile image,
        [FromServices] OcrService ocr,
        [FromServices] TransactionParser parser)
    {
        if (image == null || image.Length == 0)
            return Content("No image uploaded", "text/plain");

        using var stream = image.OpenReadStream();
        var text = ocr.ExtractText(stream);
        var parsed = parser.Parse(text);

        var status = "Unknown";
        if (parsed != null)
        {
            if (parsed.IsSuccessful) status = "SUCCESS";
            else if (parsed.IsFailed) status = "FAILED";
        }

        var result = $@"
=== RAW OCR TEXT ===
{text}

=== PARSED DATA ===
Amount: {parsed?.Amount}
Reference: {parsed?.Reference}
Date: {parsed?.Date}
Status: {status}
From: {parsed?.From}
Recipient Account: {parsed?.RecipientAccount}
";

        return Content(result, "text/plain", System.Text.Encoding.UTF8);
    }
}