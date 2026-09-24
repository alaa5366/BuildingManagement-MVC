using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using ClosedXML.Excel;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
public class BackupController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly UsersService _users;
    private readonly FirebaseAuthRestService _fbAuth;
    private readonly ExcelTemplateService _template;
    private readonly ExcelImportService _import;
    private readonly ILogger<BackupController> _logger;

    public BackupController(
        BuildingsService buildings,
        UsersService users,
        FirebaseAuthRestService fbAuth,
        ExcelTemplateService template,
        ExcelImportService import,
        ILogger<BackupController> logger)
    {
        _buildings = buildings;
        _users = users;
        _fbAuth = fbAuth;
        _template = template;
        _import = import;
        _logger = logger;
    }

    // ============================================================
    // الصفحة الرئيسية
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var buildings = await _buildings.GetAllAsync();
        ViewBag.Buildings = buildings;
        return View();
    }

    // ============================================================
    // 1. تنزيل القالب الفاضي
    // ============================================================
    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        try
        {
            var issuedBy = User.Identity?.Name ?? "superadmin";
            var bytes = _template.GenerateTemplate(null, issuedBy);

            var fileName = $"building-import-template-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] DownloadTemplate failed");
            TempData["Error"] = "فشل تنزيل القالب: " + ex.Message;
            return RedirectToAction("Index");
        }
    }

    // ============================================================
    // 2. رفع ملف Excel
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadExcel(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["Error"] = "لم يتم رفع أي ملف";
            return RedirectToAction("Index");
        }

        var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            TempData["Error"] = "الملف لازم يكون بصيغة .xlsx";
            return RedirectToAction("Index");
        }

        try
        {
            using var stream = excelFile.OpenReadStream();
            var result = _import.ValidateFile(stream);

            if (!result.IsSuccess || result.Data == null)
            {
                TempData["Error"] = result.Error ?? "الملف غير صالح";
                return RedirectToAction("Index");
            }

            var json = System.Text.Json.JsonSerializer.Serialize(result.Data);
            TempData["ImportDataJson"] = json;

            return View("ConfirmImport", result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] UploadExcel failed");
            TempData["Error"] = "فشل قراءة الملف: " + ex.Message;
            return RedirectToAction("Index");
        }
    }

    // ============================================================
    // 3. تأكيد الاستيراد (بدون رفع الملف تاني)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmImport()
    {
        var json = TempData["ImportDataJson"] as string;
        if (string.IsNullOrEmpty(json))
        {
            TempData["Error"] = "انتهت صلاحية الجلسة. ارفع الملف تاني.";
            return RedirectToAction("Index");
        }

        BuildingImportData? data;
        try
        {
            data = System.Text.Json.JsonSerializer.Deserialize<BuildingImportData>(json);
        }
        catch
        {
            TempData["Error"] = "البيانات تالفة. ارفع الملف تاني.";
            return RedirectToAction("Index");
        }

        if (data == null || data.Apartments.Count == 0)
        {
            TempData["Error"] = "البيانات ناقصة. ارفع الملف تاني.";
            return RedirectToAction("Index");
        }

        try
        {
            // ============================================================
            // ✅ 1. إنشاء العمارة
            // ============================================================
            var (buildingId, buildingNumber) = await _buildings.CreateAsync(
                data.Name,
                data.AdminPin,
                data.AdminWhatsapp,
                data.LogoUrl,
                data.BuildingNumber,
                addDefaultExpenseCategories: true,
                addDefaultRevenueCategories: true
            );

            var building = await _buildings.GetByIdAsync(buildingId);
            if (building == null)
            {
                TempData["Error"] = "فشل إنشاء العمارة";
                return RedirectToAction("Index");
            }

            // ============================================================
            // ✅ 2. تجميع الشقق حسب الدور + إنشاء Firebase Auth accounts
            // ============================================================
            var floorsGrouped = data.Apartments
                .GroupBy(a => new { a.FloorLabel, a.FloorOrder })
                .OrderBy(g => g.Key.FloorOrder)
                .ToList();

            foreach (var group in floorsGrouped)
            {
                var floorId = Guid.NewGuid().ToString("N");
                building.Floors.Add(new Floor
                {
                    Id = floorId,
                    Label = group.Key.FloorLabel,
                    Order = group.Key.FloorOrder
                });

                foreach (var aptData in group.OrderBy(a => a.AptNumber))
                {
                    var phone = AuthHelpers.NormalizePhone(aptData.WhatsApp);
                    var aptId = Guid.NewGuid().ToString("N");

                    building.Apartments.Add(new Apartment
                    {
                        Id = aptId,
                        FloorId = floorId,
                        Number = aptData.AptNumber,
                        Owner = aptData.Owner,
                        Phone = phone,
                        MonthlyFee = aptData.MonthlyFee,
                        Pin = aptData.Pin,
                        Closed = aptData.Status.Equals("closed", StringComparison.OrdinalIgnoreCase),
                        Label = aptData.AptLabel,
                        Notes = aptData.Notes,
                        CloseDate = aptData.Status.Equals("closed", StringComparison.OrdinalIgnoreCase)
                            ? DateTime.UtcNow.ToString("o")
                            : null,
                        OpenDate = DateTime.UtcNow.ToString("o")
                    });

                    // ✅ إنشاء Firebase Auth account للشقة
                    try
                    {
                        var email = AuthHelpers.ResidentInternalEmail(buildingId, group.Key.FloorOrder, aptData.AptNumber);
                        var password = AuthHelpers.ResidentPassword(buildingId, aptData.AptNumber, aptData.Pin);
                        await _fbAuth.CreateUserAsync(email, password);
                        Console.WriteLine($"[Backup] Resident auth created: {email}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"[Backup] Failed to create auth for apt {aptData.AptNumber}");
                    }
                }
            }

            // ============================================================
            // ✅ 3. حفظ العمارة (مع الشقق والأدوار)
            // ============================================================
            await _buildings.SaveFullAsync(building);
            Console.WriteLine($"[Backup] Building saved: {buildingId}");

            // ============================================================
            // ✅ 4. إنشاء Firebase Auth account للأدمن
            // ============================================================
            string? adminUid = null;
            string adminEmail;
            try
            {
                adminEmail = AuthHelpers.AdminEmailForPhone(buildingId, building.AdminWhatsapp);
                var adminPassword = AuthHelpers.AdminPasswordForPhone(building.AdminWhatsapp, building.AdminPin);

                Console.WriteLine($"[Backup] Creating admin auth: {adminEmail}");
                var createResult = await _fbAuth.CreateUserAsync(adminEmail, adminPassword);
                Console.WriteLine($"[Backup] Admin auth result: {createResult}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Backup] Failed to create admin auth");
                adminEmail = AuthHelpers.AdminEmailForPhone(buildingId, building.AdminWhatsapp);
            }

            // ============================================================
            // ✅ 5. تسجيل الأدمن في users collection
            // ============================================================
            try
            {
                // signin الأول عشان نجيب الـ UID
                var adminPassword = AuthHelpers.AdminPasswordForPhone(building.AdminWhatsapp, building.AdminPin);
                var signIn = await _fbAuth.SignInWithPasswordAsync(adminEmail, adminPassword);

                if (signIn.Success && !string.IsNullOrEmpty(signIn.Uid))
                {
                    adminUid = signIn.Uid;
                    Console.WriteLine($"[Backup] Admin signed in, UID: {adminUid}");

                    await _users.SetAsync(adminUid, new Dictionary<string, object>
                    {
                        ["email"] = adminEmail,
                        ["name"] = "Admin - " + building.Name,
                        ["phone"] = building.AdminWhatsapp,
                        ["pin"] = building.AdminPin,
                        ["role"] = "admin",
                        ["buildingIds"] = new List<object> { buildingId }
                    });

                    Console.WriteLine($"[Backup] Admin added to users collection");
                }
                else
                {
                    _logger.LogWarning($"[Backup] Admin signin failed: {signIn.Error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Backup] Failed to add admin to users");
            }

            TempData["Message"] = $"✅ تم استيراد العمارة '{data.Name}' بنجاح ({data.Apartments.Count} شقة)";
            return RedirectToAction("Details", "Buildings", new { id = buildingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] ConfirmImport failed");
            TempData["Error"] = "فشل الاستيراد: " + ex.Message;
            return RedirectToAction("Index");
        }
    }

    // ============================================================
    // 4. تصدير عمارة واحدة JSON
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> ExportJson(string id)
    {
        var building = await _buildings.GetByIdAsync(id);
        if (building == null) return NotFound();

        var json = System.Text.Json.JsonSerializer.Serialize(building, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fileName = $"{building.BuildingNumber}-{DateTime.Now:yyyyMMdd-HHmm}.json";

        return File(bytes, "application/json", fileName);
    }

    // ============================================================
    // 5. تصدير كل العمارات JSON
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> ExportAllJson()
    {
        var buildings = await _buildings.GetAllAsync();

        var wrapper = new
        {
            exportedAt = DateTime.UtcNow.ToString("o"),
            exportedBy = User.Identity?.Name ?? "superadmin",
            count = buildings.Count,
            buildings
        };

        var json = System.Text.Json.JsonSerializer.Serialize(wrapper, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fileName = $"all-buildings-{DateTime.Now:yyyyMMdd-HHmm}.json";

        return File(bytes, "application/json", fileName);
    }

    // ============================================================
    // 6. تصدير Excel
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> ExportExcel(string id)
    {
        var building = await _buildings.GetByIdAsync(id);
        if (building == null) return NotFound();

        using var workbook = new XLWorkbook();

        var infoSheet = workbook.Worksheets.Add("Building Info");
        infoSheet.Cell(1, 1).Value = "الحقل";
        infoSheet.Cell(1, 2).Value = "القيمة";
        infoSheet.Cell(2, 1).Value = "Building Name";
        infoSheet.Cell(2, 2).Value = building.Name;
        infoSheet.Cell(3, 1).Value = "Building Number";
        infoSheet.Cell(3, 2).Value = building.BuildingNumber;
        infoSheet.Cell(4, 1).Value = "Admin PIN";
        infoSheet.Cell(4, 2).Value = building.AdminPin;
        infoSheet.Cell(5, 1).Value = "Admin WhatsApp";
        infoSheet.Cell(5, 2).Value = building.AdminWhatsapp;
        infoSheet.Cell(6, 1).Value = "Logo URL";
        infoSheet.Cell(6, 2).Value = building.LogoUrl;

        var aptsSheet = workbook.Worksheets.Add("Floors & Apartments");
        var headers = new[]
        {
            "Floor Label", "Floor Order", "Apt Number", "Owner",
            "WhatsApp", "PIN", "Monthly Fee", "Apt Label",
            "Status", "Closed Reason", "Notes"
        };
        for (int i = 0; i < headers.Length; i++)
            aptsSheet.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var floor in building.Floors.OrderBy(f => f.Order))
        {
            var apts = building.Apartments
                .Where(a => a.FloorId == floor.Id)
                .OrderBy(a => a.Number)
                .ToList();

            foreach (var apt in apts)
            {
                aptsSheet.Cell(row, 1).Value = floor.Label;
                aptsSheet.Cell(row, 2).Value = floor.Order;
                aptsSheet.Cell(row, 3).Value = apt.Number;
                aptsSheet.Cell(row, 4).Value = apt.Owner;
                aptsSheet.Cell(row, 5).Value = apt.Phone;
                aptsSheet.Cell(row, 6).Value = apt.Pin;
                aptsSheet.Cell(row, 7).Value = apt.MonthlyFee;
                aptsSheet.Cell(row, 8).Value = apt.Label;
                aptsSheet.Cell(row, 9).Value = apt.Closed ? "closed" : "open";
                aptsSheet.Cell(row, 10).Value = "";
                aptsSheet.Cell(row, 11).Value = apt.Notes;
                row++;
            }
        }

        var expSheet = workbook.Worksheets.Add("Expense Categories");
        expSheet.Cell(1, 1).Value = "Name";
        expSheet.Cell(1, 2).Value = "Color";
        expSheet.Cell(1, 3).Value = "Active";
        expSheet.Cell(1, 4).Value = "ملاحظات";
        row = 2;
        foreach (var cat in building.ExpenseCategories)
        {
            expSheet.Cell(row, 1).Value = cat.Name;
            expSheet.Cell(row, 2).Value = cat.Color;
            expSheet.Cell(row, 3).Value = cat.Active;
            row++;
        }

        var revSheet = workbook.Worksheets.Add("Revenue Categories");
        revSheet.Cell(1, 1).Value = "Name";
        revSheet.Cell(1, 2).Value = "Color";
        revSheet.Cell(1, 3).Value = "Active";
        revSheet.Cell(1, 4).Value = "ملاحظات";
        row = 2;
        foreach (var cat in building.RevenueCategories)
        {
            revSheet.Cell(row, 1).Value = cat.Name;
            revSheet.Cell(row, 2).Value = cat.Color;
            revSheet.Cell(row, 3).Value = cat.Active;
            row++;
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);

        var fileName = $"{building.BuildingNumber}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}