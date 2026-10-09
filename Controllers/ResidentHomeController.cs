using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "resident")]
public class ResidentHomeController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly PollsService _polls;
    private readonly MaintenanceService _maint;
    private readonly NotificationsService _notify;

    public ResidentHomeController(
        BuildingsService buildings,
        WalletService wallet,
        PollsService polls,
        MaintenanceService maint,
        NotificationsService notify)
    {
        _buildings = buildings;
        _wallet = wallet;
        _polls = polls;
        _maint = maint;
        _notify = notify;
    }

    public async Task<IActionResult> Index()
    {
        var buildingId = User.FindFirstValue("buildingId");
        var aptId = User.FindFirstValue("apartmentId");
        var aptNumber = User.FindFirstValue("apartmentNumber");

        var building = buildingId == null ? null : await _buildings.GetByIdAsync(buildingId);

        Apartment? apt = null;
        if (building != null)
        {
            if (!string.IsNullOrWhiteSpace(aptId))
                apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);

            if (apt == null && !string.IsNullOrWhiteSpace(aptNumber))
                apt = building.Apartments.FirstOrDefault(a => a.Number.ToString() == aptNumber);
        }

        ViewBag.Building = building;

        if (building == null || apt == null)
        {
            ViewBag.HasData = false;
            return View((Apartment?)null);
        }

        ViewBag.HasData = true;

        var currentMonth = WalletService.CurrentMonthKey();

        // ============================================================
        // 1. المالية
        // ============================================================
        var balance = _wallet.ComputeWalletBalance(building, apt.Id, currentMonth);

        // ============================================================
        // 2. كل الحركات (لحساب الإحصائيات)
        // ============================================================
        var allTx = _wallet.GetUnifiedTransactions(building, apt.Id, currentMonth);

        // ============================================================
        // 3. الإحصائيات — مدفوع الشهر ده فقط
        // ============================================================
        var paidThisMonth = allTx
            .Where(t => t.Type == "deposit"
                     && t.Status == "confirmed"
                     && !string.IsNullOrEmpty(t.CreatedAt)
                     && t.CreatedAt.StartsWith(currentMonth, StringComparison.Ordinal))
            .Sum(t => t.Amount);

        var totalPaidAllTime = allTx
            .Where(t => t.Type == "deposit" && t.Status == "confirmed")
            .Sum(t => t.Amount);

        var totalExpenses = Math.Abs(allTx
            .Where(t => t.Type == "expense")
            .Sum(t => t.Amount));

        var totalRevenue = allTx
            .Where(t => t.Type == "revenue")
            .Sum(t => t.Amount);

        var monthsCount = building.Months?.Count ?? 0;

        // ============================================================
        // 4. آخر 5 حركات (بدون carry_over)
        // ============================================================
        var timelineTx = allTx
            .Where(t => t.Type != "carry_over")
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .ToList();

        // ============================================================
        // 5. الدفعات المعلقة (آخر 3)
        // ============================================================
        var pendingDeposits = _wallet.GetApartmentDeposits(building, apt.Id, currentMonth)
            .Where(d => d.Status == "pending")
            .OrderByDescending(d => d.CreatedAt)
            .Take(3)
            .ToList();

        // ============================================================
        // 6. التصويتات النشطة (آخر 3)
        // ============================================================
        var activePolls = _polls.GetPolls(building)
            .Where(p => PollsService.IsPollActive(p))
            .Take(3)
            .ToList();

        // ============================================================
        // 7. آخر 3 إشعارات + unread
        // ============================================================
        var notifications = _notify.GetForResident(building, apt.Id, 3)
            .OrderByDescending(n => n.Ts)
            .Take(3)
            .ToList();

        var unreadCount = _notify.UnreadCountForResident(building, apt.Id);

        // ============================================================
        // 8. آخر 3 صيانة
        // ============================================================
        var maintenance = _maint.GetLog(building)
            .Where(m => m.Status != "cancelled")
            .Take(3)
            .ToList();

        // ============================================================
        // 9. آخر دفعة (للـ Hero) — الشهر الحالي فقط
        // ============================================================
        var lastConfirmedDeposit = allTx
            .Where(t => t.Type == "deposit"
                     && t.Status == "confirmed"
                     && !string.IsNullOrEmpty(t.CreatedAt)
                     && t.CreatedAt.StartsWith(currentMonth, StringComparison.Ordinal))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault();

        // ============================================================
        // 10. Progress Bar
        // ============================================================
        var expectedMonthly = apt.MonthlyFee;

        var progressPercent = expectedMonthly > 0
            ? Math.Min(100, (int)Math.Round((paidThisMonth / expectedMonthly) * 100))
            : 0;

        // ============================================================
        // 11. أقدم إشعار غير مقروء
        // ============================================================
        var oldestUnread = _notify.GetForResident(building, apt.Id, 100)
            .Where(n => !n.Read)
            .OrderBy(n => n.Ts)
            .FirstOrDefault();

        var unreadDays = 0;
        if (oldestUnread != null && DateTime.TryParse(oldestUnread.Ts, out var dtUnread))
        {
            unreadDays = (int)(DateTime.UtcNow - dtUnread.ToUniversalTime()).TotalDays;
        }

        // ============================================================
        // ViewBag
        // ============================================================
        ViewBag.CurrentMonth = currentMonth;
        ViewBag.Balance = balance;

        // Stats
        ViewBag.PaidThisMonth = paidThisMonth;
        ViewBag.TotalPaidAllTime = totalPaidAllTime;
        ViewBag.TotalExpenses = totalExpenses;
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.MonthsCount = monthsCount;

        // Lists
        ViewBag.TimelineTransactions = timelineTx;
        ViewBag.PendingDeposits = pendingDeposits;
        ViewBag.ActivePolls = activePolls;
        ViewBag.Notifications = notifications;
        ViewBag.UnreadCount = unreadCount;
        ViewBag.Maintenance = maintenance;

        // Hero
        ViewBag.LastDepositAmount = lastConfirmedDeposit?.Amount ?? 0;
        ViewBag.LastDepositDate = lastConfirmedDeposit?.CreatedAt ?? "";

        // Progress
        ViewBag.ExpectedMonthly = expectedMonthly;
        ViewBag.ProgressPercent = progressPercent;

        // Action Center
        ViewBag.UnreadDays = unreadDays;

        return View(apt);
    }

    [HttpGet]
    public IActionResult Debug()
    {
        var claims = string.Join("\n", User.Claims.Select(c => $"{c.Type} = {c.Value}"));
        var isAuth = User.Identity?.IsAuthenticated ?? false;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "(none)";

        var text = $@"IsAuthenticated: {isAuth}
Role: {role}

=== Claims ===
{claims}";

        return Content(text, "text/plain", System.Text.Encoding.UTF8);
    }
}