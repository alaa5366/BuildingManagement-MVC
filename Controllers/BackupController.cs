using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[HasPermission(AdminPermissions.ManageBackup)]
public class BackupController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly UsersService _users;
    private readonly ExcelTemplateService _template;
    private readonly ExcelImportService _import;
    private readonly BackupService _backup;
    private readonly ScheduledBackupService _scheduled;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BackupController> _logger;

    public BackupController(
        BuildingsService buildings,
        UsersService users,
        ExcelTemplateService template,
        ExcelImportService import,
        BackupService backup,
        ScheduledBackupService scheduled,
        IWebHostEnvironment env,
        ILogger<BackupController> logger)
    {
        _buildings = buildings;
        _users = users;
        _template = template;
        _import = import;
        _backup = backup;
        _scheduled = scheduled;
        _env = env;
        _logger = logger;
    }

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    // ═══════════════════════════════════════════════════════════
    // Index
    // ═══════════════════════════════════════════════════════════
    [HttpGet]
    public async Task<IActionResult> Index(string? tab = "overview")
    {
        var buildings = await _buildings.GetAllAsync();
        var stats = await _backup.GetStatsAsync();
        var backupFiles = _scheduled.ListBackupFiles(_env);
        var settings = await _scheduled.GetSettingsAsync();
        var history = await _scheduled.GetHistoryAsync(_env, 50);

        ViewBag.Buildings = buildings;
        ViewBag.Stats = stats;
        ViewBag.BackupFiles = backupFiles;
        ViewBag.Settings = settings;
        ViewBag.History = history;
        ViewBag.ActiveTab = tab;

        ViewBag.TotalCollections = stats.Count;
        ViewBag.TotalDocuments = stats.Sum(s => s.DocCount);
        ViewBag.TotalBuildings = buildings.Count;
        ViewBag.TotalBackups = backupFiles.Count;
        ViewBag.LastScheduledRun = settings.LastRunAt;
        ViewBag.LastScheduledStatus = settings.LastRunStatus;
        ViewBag.AvailableCollections = BackupService.GetKnownCollections();

        return View();
    }

    // ═══════════════════════════════════════════════════════════
    // Backup ZIP
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(List<string> collections)
    {
        if (collections == null || collections.Count == 0)
        {
            TempData["Error"] = Loc.T("Select_At_Least_One_Collection");
            return RedirectToAction(nameof(Index), new { tab = "backup" });
        }

        try
        {
            var (bytes, result) = await _backup.CreateBackupAsync(collections, CurrentUserId);
            TempData["Message"] = Loc.T("Backup_Created_N_Collections_N_Documents", result.Collections.Count, result.TotalDocuments);
            return File(bytes, "application/zip", result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup create failed");
            TempData["Error"] = Loc.T("Creation_Failed") + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "backup" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Restore
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> PreviewRestore(IFormFile backupFile)
    {
        if (backupFile == null || backupFile.Length == 0)
        {
            TempData["Error"] = Loc.T("Upload_A_ZIP_File_First");
            return RedirectToAction(nameof(Index), new { tab = "restore" });
        }

        if (!backupFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = Loc.T("The_File_Must_Be_A_ZIP");
            return RedirectToAction(nameof(Index), new { tab = "restore" });
        }

        try
        {
            using var stream = backupFile.OpenReadStream();
            var preview = await _backup.PreviewRestoreAsync(stream, backupFile.FileName, CurrentUserId);

            if (!preview.Success)
            {
                TempData["Error"] = Loc.T("Failed_To_Read_The_File") + string.Join(Loc.T("Text_8a78cc"), preview.Errors);
                return RedirectToAction(nameof(Index), new { tab = "restore" });
            }

            return View("RestorePreview", preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup preview failed");
            TempData["Error"] = Loc.T("Failed_5") + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "restore" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteRestore(
        string tempFilePath, List<string> collections, bool overwrite)
    {
        if (string.IsNullOrEmpty(tempFilePath) || collections == null || collections.Count == 0)
        {
            TempData["Error"] = Loc.T("Incomplete_Data");
            return RedirectToAction(nameof(Index), new { tab = "restore" });
        }

        try
        {
            var result = await _backup.RestoreAsync(tempFilePath, collections, overwrite, CurrentUserId);
            return View("RestoreResult", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed");
            TempData["Error"] = Loc.T("Failed_5") + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "restore" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Scheduled
    // ═══════════════════════════════════════════════════════════
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveScheduledSettings(ScheduledBackupSettings model)
    {
        if (model.Enabled && (model.Collections == null || model.Collections.Count == 0))
        {
            TempData["Error"] = Loc.T("You_Must_Select_At_Least_One");
            return RedirectToAction(nameof(Index), new { tab = "scheduled" });
        }

        try
        {
            await _scheduled.SaveSettingsAsync(model, CurrentUserId);
            TempData["Message"] = Loc.T("Settings_Saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveScheduledSettings failed");
            TempData["Error"] = Loc.T("Save_Failed_2") + ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = "scheduled" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunNow()
    {
        try
        {
            await _scheduled.RunScheduledBackupAsync(_env, forceRun: true);
            TempData["Message"] = Loc.T("Backup_Created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RunNow failed");
            TempData["Error"] = Loc.T("Failed_5") + ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "scheduled" });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadBackup(string fileName)
    {
        var path = await _scheduled.GetBackupFilePathAsync(_env, fileName);
        if (path == null) return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        return File(bytes, "application/zip", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBackup(string fileName)
    {
        var ok = await _scheduled.DeleteBackupAsync(_env, fileName, CurrentUserId);
        TempData[ok ? "Message" : "Error"] = ok ? Loc.T("Deleted_2") : Loc.T("File_Not_Found");
        return RedirectToAction(nameof(Index), new { tab = "scheduled" });
    }

    // ═══════════════════════════════════════════════════════════
    // Excel
    // ═══════════════════════════════════════════════════════════
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
            TempData["Error"] = Loc.T("Failed_To_Download_The_Template") + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "import" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadExcel(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["Error"] = Loc.T("No_File_Was_Uploaded");
            return RedirectToAction(nameof(Index), new { tab = "import" });
        }

        var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
        {
            TempData["Error"] = Loc.T("The_File_Must_Be_In_Xlsx");
            return RedirectToAction(nameof(Index), new { tab = "import" });
        }

        try
        {
            using var stream = excelFile.OpenReadStream();
            var result = _import.ValidateFile(stream);

            if (!result.IsSuccess || result.Data == null)
            {
                TempData["Error"] = result.Error ?? Loc.T("The_File_Is_Invalid");
                return RedirectToAction(nameof(Index), new { tab = "import" });
            }

            var json = System.Text.Json.JsonSerializer.Serialize(result.Data);
            TempData["ImportDataJson"] = json;

            return View("ConfirmImport", result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] UploadExcel failed");
            TempData["Error"] = Loc.T("Failed_To_Read_The_File") + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "import" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmImport(
    [FromServices] ExcelImportService importService)
    {
        var json = TempData["ImportDataJson"] as string;
        if (string.IsNullOrEmpty(json))
        {
            TempData["Error"] = Loc.T("The_Session_Has_Expired_Upload_The");
            return RedirectToAction(nameof(Index), new { tab = "import" });
        }

        BuildingImportData? data;
        try
        {
            data = System.Text.Json.JsonSerializer.Deserialize<BuildingImportData>(json);
        }
        catch
        {
            TempData["Error"] = Loc.T("The_Data_Is_Corrupted_Upload_The");
            return RedirectToAction(nameof(Index), new { tab = "import" });
        }

        if (data == null || data.Apartments.Count == 0)
        {
            TempData["Error"] = Loc.T("The_Data_Is_Incomplete_Upload_The");
            return RedirectToAction(nameof(Index), new { tab = "import" });
        }

        try
        {
            if (string.IsNullOrWhiteSpace(data.BuildingNumber))
            {
                data.BuildingNumber = await importService.GenerateBuildingNumberAsync();
            }

            var (buildingId, buildingNumber) = await _buildings.CreateAsync(
                data.Name, data.AdminPin, data.AdminWhatsapp, data.LogoUrl, data.BuildingNumber,
                addDefaultExpenseCategories: true,
                addDefaultRevenueCategories: true);

            var building = await _buildings.GetByIdAsync(buildingId);
            if (building == null)
            {
                TempData["Error"] = Loc.T("Failed_To_Create_The_Building");
                return RedirectToAction(nameof(Index), new { tab = "import" });
            }

            var floorsGrouped = data.Apartments
                .GroupBy(a => new { a.FloorLabel, a.FloorOrder })
                .OrderBy(g => g.Key.FloorOrder)
                .ToList();

            int globalAptNumber = 1;

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

                    int finalAptNumber = aptData.AptNumber > 0
                        ? aptData.AptNumber
                        : globalAptNumber++;

                    building.Apartments.Add(new Apartment
                    {
                        Id = aptId,
                        FloorId = floorId,
                        Number = finalAptNumber,
                        Owner = aptData.Owner,
                        Phone = phone,
                        MonthlyFee = aptData.MonthlyFee,
                        Pin = aptData.Pin,
                        Closed = aptData.Status.Equals("closed", StringComparison.OrdinalIgnoreCase),
                        Label = aptData.AptLabel,
                        Notes = aptData.Notes,
                        CloseDate = aptData.Status.Equals("closed", StringComparison.OrdinalIgnoreCase)
                            ? DateTime.UtcNow.ToString("o") : null,
                        OpenDate = DateTime.UtcNow.ToString("o"),
                        Disabled = aptData.IsDisabled,
                        DisabledReason = aptData.IsDisabled ? aptData.ClosedReason : ""
                    });
                }
            }

            await _buildings.SaveFullAsync(building);

            TempData["Message"] = Loc.T("Building_N_Imported_Successfully_N_Apartments",
                data.Name, data.Apartments.Count);
            return RedirectToAction("Details", "Buildings", new { id = buildingId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] ConfirmImport failed");
            TempData["Error"] = Loc.T("Import_Failed") + ex.Message;
            return RedirectToAction(nameof(Index), new { tab = "import" });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Export
    // ═══════════════════════════════════════════════════════════
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
            var apts = building.Apartments.Where(a => a.FloorId == floor.Id).OrderBy(a => a.Number).ToList();
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