using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[HasPermission(AdminPermissions.ManageTools)]
public class ToolsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly UsersService _users;
    private readonly MigrationService _migration;
    private readonly DbMaintenanceService _maintenance;
    private readonly IAuditLogger _audit;

    public ToolsController(
        BuildingsService buildings,
        UsersService users,
        MigrationService migration,
        DbMaintenanceService maintenance,
        IAuditLogger audit)
    {
        _buildings = buildings;
        _users = users;
        _migration = migration;
        _maintenance = maintenance;
        _audit = audit;
    }

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    // ============================================================
    // الرئيسية
    // ============================================================
    public IActionResult Index()
    {
        var buildings = _buildings.GetAllAsync().Result;
        ViewBag.Buildings = buildings;
        return View();
    }

    // ============================================================
    // Migration
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Migration()
    {
        ViewBag.Buildings = await _buildings.GetAllAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunMigration(string? buildingId, bool dryRun)
    {
        MigrationResult result;

        if (string.IsNullOrWhiteSpace(buildingId))
            result = await _migration.MigrateAllAsync(dryRun, CurrentUserId);
        else
            result = await _migration.MigrateBuildingAsync(buildingId, dryRun, CurrentUserId);

        ViewBag.Result = result;
        ViewBag.IsDryRun = dryRun;
        ViewBag.Buildings = await _buildings.GetAllAsync();

        return View("Migration");
    }

    // ============================================================
    // Maintenance
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Maintenance()
    {
        ViewBag.Buildings = await _buildings.GetAllAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunMaintenance(string tool, bool fix, string? buildingId, int olderThanDays = 90)
    {
        MaintenanceResult result;

        switch (tool)
        {
            case "duplicate-users":
                result = await _maintenance.FindDuplicateUsersAsync(fix);
                break;
            case "stale-deposits":
                result = await _maintenance.FindStalePendingDepositsAsync(olderThanDays, fix);
                break;
            case "recalc-balances":
                if (string.IsNullOrWhiteSpace(buildingId))
                {
                    TempData["Error"] = "اختر عمارة أولاً";
                    return RedirectToAction(nameof(Maintenance));
                }
                result = await _maintenance.RecalculateBalancesAsync(buildingId);
                break;
            case "orphaned-deposits":
                result = await _maintenance.FindOrphanedDepositsAsync(fix);
                break;
            default:
                TempData["Error"] = "أداة غير معروفة";
                return RedirectToAction(nameof(Maintenance));
        }

        await _audit.LogAsync(
            action: $"tools.maintenance.{tool}",
            buildingId: buildingId,
            userId: CurrentUserId,
            userRole: "superadmin",
            metadata: new { found = result.ItemsFound, fixedCount = result.ItemsFixed, fix },
            severity: "warning");

        ViewBag.Result = result;
        ViewBag.Fix = fix;
        ViewBag.Buildings = await _buildings.GetAllAsync();
        ViewBag.SelectedTool = tool;
        ViewBag.BuildingId = buildingId;
        ViewBag.OlderThanDays = olderThanDays;

        return View("Maintenance");
    }

    // ============================================================
    // تصدير شامل ZIP
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Export()
    {
        ViewBag.Buildings = await _buildings.GetAllAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportAll()
    {
        var buildings = await _buildings.GetAllAsync();
        var users = await _users.GetAdminsAsync();

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            // buildings.json
            var buildingsJson = JsonSerializer.Serialize(buildings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            AddFileToZip(zip, "buildings.json", buildingsJson);

            // users.json
            var usersJson = JsonSerializer.Serialize(users, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            AddFileToZip(zip, "users.json", usersJson);

            // README
            var readme = $@"# Export from Building Management System
تاريخ التصدير: {DateTime.UtcNow:O}

## المحتوى:
- buildings.json: كل العمارات ({buildings.Count})
- users.json: كل الأدمنة ({users.Count})

## ملاحظات:
- الملفات JSON يمكن استيرادها لاحقًا.
- للدعم: alaa5366@gmail.com
";
            AddFileToZip(zip, "README.md", readme);
        }

        await _audit.LogAsync(
            action: "tools.export.all",
            userId: CurrentUserId,
            userRole: "superadmin",
            metadata: new { buildingsCount = buildings.Count, usersCount = users.Count },
            severity: "warning");

        var fileName = $"backup-{DateTime.UtcNow:yyyy-MM-dd-HHmm}.zip";
        return File(ms.ToArray(), "application/zip", fileName);
    }

    private static void AddFileToZip(ZipArchive zip, string fileName, string content)
    {
        var entry = zip.CreateEntry(fileName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }
}