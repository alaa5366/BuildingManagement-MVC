using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
public class BuildingsController : Controller
{
    private readonly BuildingsService _service;
    private readonly QrCodePdfService _qrPdf;

    public BuildingsController(BuildingsService service, QrCodePdfService qrPdf)
    {
        _service = service;
        _qrPdf = qrPdf;
    }

    public async Task<IActionResult> Index()
    {
        var buildings = await _service.GetAllAsync();
        return View(buildings);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string name,
        string? buildingNumber,
        string adminPin,
        string adminWhatsapp,
        string? logoUrl,
        bool addDefaultExpenseCategories = false,
        bool addDefaultRevenueCategories = false)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
        {
            ModelState.AddModelError("", "اسم العمارة مطلوب (من 2 إلى 100 حرف)");
            return View();
        }

        if (string.IsNullOrWhiteSpace(adminPin) || adminPin.Length != 4 || !adminPin.All(char.IsDigit))
        {
            ModelState.AddModelError("", "PIN الأدمن لازم يكون 4 أرقام بالظبط");
            return View();
        }

        if (string.IsNullOrWhiteSpace(adminWhatsapp))
        {
            ModelState.AddModelError("", "رقم واتساب الأدمن مطلوب");
            return View();
        }

        var phoneClean = adminWhatsapp.Replace("+", "").Trim();
        if (!phoneClean.All(char.IsDigit) || phoneClean.Length < 7)
        {
            ModelState.AddModelError("", "رقم واتساب الأدمن غير صحيح (أرقام فقط، 7 خانات على الأقل)");
            return View();
        }

        if (!string.IsNullOrWhiteSpace(buildingNumber))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(buildingNumber.Trim(), @"^BLD-\d{3}$"))
            {
                ModelState.AddModelError("", "رقم العمارة لازم يكون بصيغة BLD-XXX (مثال: BLD-001)");
                return View();
            }
        }

        var (id, number) = await _service.CreateAsync(
            name.Trim(),
            adminPin.Trim(),
            adminWhatsapp.Trim(),
            logoUrl?.Trim(),
            buildingNumber?.Trim(),
            addDefaultExpenseCategories,
            addDefaultRevenueCategories
        );

        TempData["Message"] = $"✅ تم إنشاء العمارة {number} بنجاح";
        return RedirectToAction("Details", new { id });
    }

    public async Task<IActionResult> Details(string id)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();
        return View(building);
    }

    // ============================================================
    // ✅ جديد: صفحة QR Codes
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> QrCodes(string id)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();
        return View(building);
    }

    // ============================================================
    // ✅ جديد: تحميل PDF فيه كل الـ QRs
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> QrCodesPdf(string id)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var pdfBytes = _qrPdf.GenerateQrCodesPdf(building, baseUrl);
        var fileName = $"QR-Codes-{building.BuildingNumber}-{DateTime.Now:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFloor(string buildingId, string label, string aptPhones, string aptPins)
    {
        var phones = (aptPhones ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pins = (aptPins ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var count = Math.Min(phones.Length, pins.Length);
        var apts = new List<(string, string)>();
        for (var i = 0; i < count; i++) apts.Add((phones[i], pins[i]));

        if (apts.Count == 0)
        {
            TempData["Error"] = "لازم تدخل رقم واتساب و PIN لكل شقة (مفصولين بفاصلة)";
            return RedirectToAction("Details", new { id = buildingId });
        }

        await _service.AddFloorAsync(buildingId, label, apts);
        TempData["Message"] = "تمت إضافة الدور بنجاح";
        return RedirectToAction("Details", new { id = buildingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddApartment(string buildingId, string floorId, string phone, string pin)
    {
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
        {
            TempData["Error"] = "رقم الواتساب و PIN (4 أرقام على الأقل) مطلوبين";
            return RedirectToAction("Details", new { id = buildingId });
        }

        await _service.AddApartmentAsync(buildingId, floorId, phone, pin);
        TempData["Message"] = "تمت إضافة الشقة بنجاح";
        return RedirectToAction("Details", new { id = buildingId });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQuickQr(
    string buildingId, string aptId,
    int validityMinutes, bool oneTime, int maxUses,
    [FromServices] QrSecurityService qrSecurity)
    {
        var building = await _service.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return NotFound();

        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);

        var options = new QuickQrOptions
        {
            BuildingId = buildingId,
            AptId = aptId,
            AptNumber = apt.Number,
            FloorOrder = floor?.Order ?? 0,
            Validity = TimeSpan.FromMinutes(validityMinutes),
            OneTime = oneTime,
            MaxUses = oneTime ? 1 : maxUses
        };

        var token = qrSecurity.GenerateQuickToken(options);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var quickUrl = $"{baseUrl}/Account/QuickLogin?token={token}";

        return Json(new
        {
            success = true,
            token,
            url = quickUrl,
            expiresIn = validityMinutes,
            oneTime,
            maxUses = options.MaxUses
        });
    }
    // ============================================================
    // إصلاح Firebase Auth Accounts (للطوارئ)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> FixFirebaseAccounts(string id,
        [FromServices] FirebaseAuthRestService fbAuth)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();

        var results = new List<object>();
        var successCount = 0;
        var failCount = 0;

        foreach (var apt in building.Apartments)
        {
            var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
            if (floor == null) continue;

            var email = AuthHelpers.ResidentInternalEmail(building.Id, floor.Order, apt.Number);
            var password = AuthHelpers.ResidentPassword(building.Id, apt.Number, apt.Pin);

            try
            {
                var result = await fbAuth.CreateUserAsync(email, password);

                if (result != null)
                {
                    successCount++;
                    results.Add(new { apt = apt.Number, email, status = "✅ Created/Updated" });
                    Console.WriteLine($"[Fix] Apt {apt.Number}: {email} — SUCCESS");
                }
                else
                {
                    failCount++;
                    results.Add(new { apt = apt.Number, email, status = "❌ Failed" });
                    Console.WriteLine($"[Fix] Apt {apt.Number}: {email} — FAILED");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                results.Add(new { apt = apt.Number, email, status = $"❌ {ex.Message}" });
                Console.WriteLine($"[Fix] Apt {apt.Number}: {email} — EXCEPTION: {ex.Message}");
            }
        }

        TempData["Message"] = $"✅ تم إصلاح {successCount} حساب، فشل {failCount}";
        return RedirectToAction("Details", new { id });
    }
    // ============================================================
    // إعادة إنشاء حسابات Firebase Auth لكل الشقق
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> RecreateFirebaseAccounts(string id,
        [FromServices] FirebaseAuthRestService fbAuth,
        [FromServices] ILogger<BuildingsController> logger)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();

        var successCount = 0;
        var failCount = 0;
        var results = new List<string>();

        foreach (var apt in building.Apartments)
        {
            var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
            if (floor == null) continue;

            var email = AuthHelpers.ResidentInternalEmail(building.Id, floor.Order, apt.Number);
            var password = AuthHelpers.ResidentPassword(building.Id, apt.Number, apt.Pin);

            // 1. جرّب SignIn الأول — لو نجح، يبقى الحساب موجود
            var signIn = await fbAuth.SignInWithPasswordAsync(email, password);
            if (signIn.Success)
            {
                successCount++;
                results.Add($"✅ شقة {apt.Number}: موجود بالفعل");
                logger.LogInformation($"[Recreate] Apt {apt.Number}: OK (already exists)");
                continue;
            }

            // 2. الحساب مش موجود أو كلمة السر غلط — اعمله جديد
            try
            {
                var created = await fbAuth.CreateUserAsync(email, password);

                // 3. بعد الإنشاء، جرّب SignIn للتأكد
                var verify = await fbAuth.SignInWithPasswordAsync(email, password);

                if (verify.Success)
                {
                    successCount++;
                    results.Add($"✅ شقة {apt.Number}: تم الإنشاء");
                    logger.LogInformation($"[Recreate] Apt {apt.Number}: Created + Verified");
                }
                else
                {
                    failCount++;
                    results.Add($"⚠️ شقة {apt.Number}: اتعمل بس SignIn فشل");
                    logger.LogWarning($"[Recreate] Apt {apt.Number}: Created but SignIn failed");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                results.Add($"❌ شقة {apt.Number}: {ex.Message}");
                logger.LogError(ex, $"[Recreate] Apt {apt.Number}: Failed");
            }
        }

        TempData["Message"] = $"✅ نجح {successCount}، فشل {failCount}";
        TempData["Details"] = string.Join(" | ", results);

        return RedirectToAction("Details", new { id });
    }
    // ============================================================
    // مزامنة كلمات سر Firebase لكل الشقق
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> SyncFirebasePasswords(string id,
        [FromServices] FirebaseAdminService fbAdmin)
    {
        var building = await _service.GetByIdAsync(id);
        if (building == null) return NotFound();

        var success = 0;
        var fail = 0;

        foreach (var apt in building.Apartments)
        {
            var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
            if (floor == null) continue;

            var email = AuthHelpers.ResidentInternalEmail(building.Id, floor.Order, apt.Number);
            var password = AuthHelpers.ResidentPassword(building.Id, apt.Number, apt.Pin);

            // 1. جرّب تحديث كلمة السر
            var updated = await fbAdmin.UpdatePasswordAsync(email, password);

            if (updated)
            {
                success++;
                Console.WriteLine($"[Sync] Apt {apt.Number}: password updated");
                continue;
            }

            // 2. لو مش موجود — اعمله
            var uid = await fbAdmin.CreateOrGetUserAsync(email, password);
            if (uid != null)
            {
                success++;
                Console.WriteLine($"[Sync] Apt {apt.Number}: created");
            }
            else
            {
                fail++;
                Console.WriteLine($"[Sync] Apt {apt.Number}: FAILED");
            }
        }

        TempData["Message"] = $"✅ نجح {success}، فشل {fail}";
        return RedirectToAction("Details", new { id });
    }
}