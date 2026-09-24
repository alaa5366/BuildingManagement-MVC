using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "resident")]
public class ResidentWalletController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly AuditLogService _audit;

    public ResidentWalletController(BuildingsService buildings, WalletService wallet, AuditLogService audit)
    {
        _buildings = buildings; _wallet = wallet; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;
    private string ApartmentId => User.FindFirstValue("apartmentId")!;

    [HttpGet]
    public async Task<IActionResult> Index(string? month)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        _wallet.GetOrCreateMonth(building, mk);

        ViewBag.Building = building;
        ViewBag.Apartment = apt;
        ViewBag.Month = mk;
        ViewBag.Balance = _wallet.ComputeWalletBalance(building, apt.Id, mk);
        ViewBag.Transactions = _wallet.GetUnifiedTransactions(building, apt.Id, mk);
        ViewBag.PendingDeposits = _wallet.GetApartmentDeposits(building, apt.Id, mk)
            .Where(d => d.Status == "pending").ToList();
        return View();
    }

    // ============================================================
    // ✅ تعديل: CreateDeposit بيستقبل صورة الإيصال
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDeposit(string month, double amount, string? note,
        IFormFile? receiptImage,
        [FromServices] CloudinaryService cloudinary)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;

        if (amount > 0)
        {
            // ✅ 1. ارفع الصورة لو موجودة
            ReceiptData? receipt = null;
            if (receiptImage != null && receiptImage.Length > 0)
            {
                using var stream = receiptImage.OpenReadStream();
                receipt = await cloudinary.UploadReceiptAsync(
                    stream,
                    receiptImage.FileName,
                    building.Id,
                    apt.Id);
            }

            // ✅ 2. أنشئ الدفعة (مع الصورة)
            var d = _wallet.CreateDeposit(building, apt.Id, amount, note, "resident", mk, receipt);

            // ✅ 3. سجّل في الـ Audit Log
            var auditDetails = $"دفعة جديدة {d.Number} — {amount:0.##} ج.م";
            if (receipt != null) auditDetails += " (مع صورة إيصال)";

            _audit.Push(building, "deposit_create", auditDetails, "resident",
                User.Identity?.Name ?? "resident", apt.Number.ToString());

            await _buildings.SaveFullAsync(building);
            TempData["Message"] = "تم إرسال الدفعة، في انتظار تأكيد الأدمن";
        }

        return RedirectToAction("Index", new { month = mk });
    }

    // ============================================================
    // قراءة صورة الإيصال بالـ OCR
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ParseReceiptImage(IFormFile image,
        [FromServices] OcrService ocr,
        [FromServices] TransactionParser parser)
    {
        if (image == null || image.Length == 0)
            return Json(new { success = false, error = "لم يتم رفع صورة" });

        if (image.Length > 10 * 1024 * 1024) // 10 MB
            return Json(new { success = false, error = "حجم الصورة كبير جداً (الحد الأقصى 10 ميجا)" });

        try
        {
            using var stream = image.OpenReadStream();
            var text = ocr.ExtractText(stream);
            var parsed = parser.Parse(text);

            if (parsed == null || parsed.Amount <= 0)
            {
                return Json(new
                {
                    success = false,
                    error = "لم نتمكن من قراءة الصورة. تأكد من وضوحها أو أدخل البيانات يدوياً.",
                    rawText = text
                });
            }

            // تحقق: هل التحويل ناجح؟
            if (!parsed.IsSuccessful)
            {
                return Json(new
                {
                    success = false,
                    error = "الصورة دي تحويل فاشل أو قيد الانتظار. ارفع صورة التحويل الناجح ✅",
                    status = "FAILED",
                    rawText = text
                });
            }

            return Json(new
            {
                success = true,
                amount = parsed.Amount,
                reference = parsed.Reference,
                date = parsed.Date,
                status = "SUCCESS",
                from = parsed.From,
                recipientAccount = parsed.RecipientAccount,
                note = BuildNote(parsed)
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "خطأ في قراءة الصورة: " + ex.Message });
        }
    }

    private static string BuildNote(TransactionData d)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(d.Date)) parts.Add($"📅 {d.Date}");
        if (!string.IsNullOrEmpty(d.Reference)) parts.Add($"🔢 Ref: {d.Reference}");
        if (!string.IsNullOrEmpty(d.RecipientAccount)) parts.Add($"🏦 Acc: {d.RecipientAccount}");
        if (!string.IsNullOrEmpty(d.From)) parts.Add($"👤 {d.From}");
        return string.Join(" | ", parts);
    }
}