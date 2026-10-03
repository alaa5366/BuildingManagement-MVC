using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[HasPermission(BuildingManagementMvc.Models.AdminPermissions.ManageTools)]
public class ToolsController : Controller
{
    private readonly MigrationService _migration;
    private readonly DbMaintenanceService _maintenance;
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(
        MigrationService migration,
        DbMaintenanceService maintenance,
        BuildingsService buildings,
        WalletService wallet,
        ILogger<ToolsController> logger)
    {
        _migration = migration;
        _maintenance = maintenance;
        _buildings = buildings;
        _wallet = wallet;
        _logger = logger;
    }

    private string CurrentUserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    public IActionResult Index() => View();

    // ============================================================
    // ✅ Migration: تحويل building.AuditLog → auditLogs collection
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunMigration(string? buildingId, bool dryRun)
    {
        try
        {
            MigrationResult result;

            if (string.IsNullOrWhiteSpace(buildingId))
            {
                // كل العمارات
                result = await _migration.MigrateAllAsync(dryRun, CurrentUserId);
            }
            else
            {
                // عمارة واحدة
                result = await _migration.MigrateBuildingAsync(buildingId, dryRun, CurrentUserId);
            }

            return View("MigrationResult", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RunMigration failed");
            TempData["Error"] = Loc.T("Migration_Failed") + ": " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // ============================================================
    // ✅ DB Maintenance: فحص وإصلاح
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunMaintenance(string tool, string? buildingId, int days = 30)
    {
        try
        {
            MaintenanceResult result = tool switch
            {
                "duplicate-users" => await _maintenance.FindDuplicateUsersAsync(fix: true),

                "old-pending" => await _maintenance.FindStalePendingDepositsAsync(
                    olderThanDays: days, cancel: true),

                "recalc-balances" => string.IsNullOrWhiteSpace(buildingId)
                    ? new MaintenanceResult
                    {
                        ToolName = Loc.T("Recalculate_Balances_2"),
                        Details = new List<string> { Loc.T("Select_A_Building_First") }
                    }
                    : await _maintenance.RecalculateBalancesAsync(buildingId),

                "orphan-refs" => await _maintenance.FindOrphanedDepositsAsync(fix: true),

                _ => new MaintenanceResult
                {
                    ToolName = Loc.T("Unknown_Tool"),
                    Details = new List<string> { tool }
                }
            };

            return View("MaintenanceResult", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RunMaintenance failed");
            TempData["Error"] = Loc.T("Error_2") + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // ============================================================
    // ✅ Migration: ترحيل الأرصدة (Carry Over)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MigrateCarryOver()
    {
        try
        {
            var allBuildings = await _buildings.GetAllAsync();
            var totalBuildings = 0;
            var totalMonths = 0;
            var totalApts = 0;

            foreach (var building in allBuildings)
            {
                var months = building.Months.Keys.OrderBy(k => k).ToList();
                var monthsUpdated = 0;

                foreach (var monthKey in months)
                {
                    var month = building.Months[monthKey];

                    // ✅ لو carryOver موجود، تخطاه
                    if (month.CarryOver != null && month.CarryOver.Count > 0)
                        continue;

                    var prevKey = GetPrevMonthKeyHelper(building, monthKey);
                    if (prevKey != null)
                    {
                        month.CarryOver = _wallet.ComputeCarryOverForNextMonth(building, prevKey);
                    }
                    else
                    {
                        month.CarryOver = new Dictionary<string, double>();
                        foreach (var apt in building.Apartments)
                            month.CarryOver[apt.Id] = 0;
                    }

                    monthsUpdated++;
                    totalApts += month.CarryOver.Count;
                }

                if (monthsUpdated > 0)
                {
                    await _buildings.SaveFullAsync(building);
                    totalBuildings++;
                    totalMonths += monthsUpdated;
                }
            }

            TempData["Message"] = Loc.T("Migration_Success_N_Buildings_N_Months_N_Apts",
                totalBuildings, totalMonths, totalApts);

            _logger.LogInformation(
                "[MigrateCarryOver] Done: {Buildings} buildings, {Months} months, {Apts} apartments",
                totalBuildings, totalMonths, totalApts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MigrateCarryOver] Failed");
            TempData["Error"] = Loc.T("Migration_Failed") + ": " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
    // ============================================================
    // ✅ تنظيف شامل: شهور غلط + carryOver غلط
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CleanupData()
    {
        try
        {
            var allBuildings = await _buildings.GetAllAsync();
            var currentMonth = WalletService.CurrentMonthKey();
            var totalCleaned = 0;
            var totalBuildings = 0;
            var details = new List<string>();

            foreach (var building in allBuildings)
            {

                // ✅ 1. حدد نقطة البداية من CreatedAt
                var createdAt = DateTime.TryParse(building.CreatedAt, out var dt)
                    ? dt.ToUniversalTime()
                    : DateTime.UtcNow;

                var firstMonthKey = createdAt.ToString("yyyy-MM");

                // ✅ 2. امسح الشهور المستقبلية
                var futureMonths = building.Months.Keys
                    .Where(k => string.Compare(k, currentMonth, StringComparison.Ordinal) > 0)
                    .ToList();
                foreach (var k in futureMonths)
                {
                    building.Months.Remove(k);
                    totalCleaned++;
                    details.Add($"🗑️ {building.Name}: removed future month {k}");
                }

                // ✅ 3. امسح الشهور قبل CreatedAt
                var beforeCreation = building.Months.Keys
                    .Where(k => string.Compare(k, firstMonthKey, StringComparison.Ordinal) < 0)
                    .ToList();
                foreach (var k in beforeCreation)
                {
                    building.Months.Remove(k);
                    totalCleaned++;
                    details.Add($"🗑️ {building.Name}: removed pre-creation month {k}");
                }

                // ✅ 4. رتب الشهور
                var months = building.Months.Keys.OrderBy(k => k).ToList();

                // ✅ 5. امسح carryOver من كل الشهور
                foreach (var monthKey in months)
                {
                    building.Months[monthKey].CarryOver = new Dictionary<string, double>();
                }

                // ✅ 6. أول شهر → carryOver = 0
                if (months.Any())
                {
                    var first = months.First();
                    foreach (var apt in building.Apartments)
                        building.Months[first].CarryOver[apt.Id] = 0;

                    details.Add($"✅ {building.Name}: first month = {first} (carryOver = 0)");
                }

                // ✅ 7. باقي الشهور → carryOver من اللي قبله
                for (int i = 1; i < months.Count; i++)
                {
                    var prevMonthKey = months[i - 1];
                    var currMonthKey = months[i];

                    var newCarry = new Dictionary<string, double>();
                    foreach (var apt in building.Apartments)
                    {
                        newCarry[apt.Id] = _wallet.ComputeWalletBalance(building, apt.Id, prevMonthKey);
                    }

                    building.Months[currMonthKey].CarryOver = newCarry;
                    details.Add($"✅ {building.Name}: {currMonthKey} carryOver computed from {prevMonthKey}");
                }

                await _buildings.SaveFullAsync(building);
                totalBuildings++;
            }

            TempData["Message"] = $"✅ Cleanup done: {totalBuildings} buildings, {totalCleaned} invalid months removed";
            TempData["CleanupDetails"] = string.Join("\n", details);
            _logger.LogInformation($"[CleanupData] Done: {totalBuildings} buildings, {totalCleaned} months removed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CleanupData] Failed");
            TempData["Error"] = "❌ " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // Helper: جلب آخر شهر قبل الشهر الحالي
    // ============================================================
    private static string? GetPrevMonthKeyHelper(Building building, string currentMonthKey)
    {
        if (string.IsNullOrWhiteSpace(currentMonthKey)) return null;

        string? prev = null;
        foreach (var key in building.Months.Keys)
        {
            if (string.Compare(key, currentMonthKey, StringComparison.Ordinal) < 0)
            {
                if (prev == null || string.Compare(key, prev, StringComparison.Ordinal) > 0)
                    prev = key;
            }
        }
        return prev;
    }
}