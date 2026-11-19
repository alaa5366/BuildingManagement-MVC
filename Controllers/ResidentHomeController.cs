using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  ResidentHomeController	لوحة تحكم الساكن (Dashboard)
// ============================================================
/// <summary>
/// Resident dashboard: current balance, payment progress, recent activity and quick links.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>resident</c>.
/// </remarks>
[Authorize(Roles = "resident")]
public class ResidentHomeController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly TargetWalletReadService _targetWallet;
    private readonly PollsService _polls;
    private readonly MaintenanceService _maint;
    private readonly TargetNotificationsService _targetNotifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResidentHomeController"/> class.
    /// </summary>
    public ResidentHomeController(
        BuildingsService buildings,
        TargetWalletReadService targetWallet,
        PollsService polls,
        MaintenanceService maint,
        TargetNotificationsService targetNotifications)
    {
        _buildings = buildings;
        _targetWallet = targetWallet;
        _polls = polls;
        _maint = maint;
        _targetNotifications = targetNotifications;
    }

    /// <summary>
    /// Shows the main page of the resident home screen.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var buildingId = User.FindFirstValue("buildingId");
        var aptId = User.FindFirstValue("apartmentId");

        var legacyBuilding = buildingId == null ? null : await _buildings.GetByIdAsync(buildingId);
        var building = string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(aptId)
            ? null
            : await _targetWallet.GetBuildingShellAsync(
                buildingId, aptId, HttpContext.RequestAborted);
        var apt = building?.Apartments.SingleOrDefault(apartment => apartment.Id == aptId);

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
        var walletSnapshot = await _targetWallet.GetApartmentAsync(building.Id, apt.Id, null, currentMonth, cancellationToken: HttpContext.RequestAborted);
        var balance = walletSnapshot.Balance;

        // ============================================================
        // 2. كل الحركات (لحساب الإحصائيات)
        // ============================================================
        var allTx = walletSnapshot.Transactions.ToList();

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

        var monthsCount = await _targetWallet.GetFinancialPeriodCountAsync(building.Id, HttpContext.RequestAborted);

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
        var pendingDeposits = (await _targetWallet.GetPendingForBuildingAsync(
                building.Id, HttpContext.RequestAborted))
            .Where(item => item.ApartmentId == apt.Id && item.PeriodKey == currentMonth)
            .OrderByDescending(item => item.SubmittedAtUtc)
            .Take(3)
            .Select(item => new Deposit
            {
                Id = item.PaymentDepositId.ToString("D"),
                Number = item.PaymentDepositId.ToString("N")[..8].ToUpperInvariant(),
                AptId = item.ApartmentId,
                Amount = item.Amount,
                Note = item.Note,
                Receipt = item.Receipt,
                Status = "pending",
                CreatedAt = item.SubmittedAtUtc.ToString("O"),
                CreatedBy = ""
            })
            .ToList();

        // ============================================================
        // 6. التصويتات النشطة (آخر 3)
        // ============================================================
        var activePolls = legacyBuilding is null
            ? new List<Poll>()
            : _polls.GetPolls(legacyBuilding)
                .Where(p => PollsService.IsPollActive(p))
                .Take(3)
                .ToList();

        // ============================================================
        // 7. آخر 3 إشعارات + unread
        // ============================================================
        var allNotifications = await _targetNotifications.GetForResidentAsync(
            building.Id, apt.Id, 100, HttpContext.RequestAborted);
        var notifications = allNotifications.Take(3).ToList();
        var unreadCount = allNotifications.Count(notification => !notification.Read);

        // ============================================================
        // 8. آخر 3 صيانة
        // ============================================================
        var maintenance = legacyBuilding is null
            ? new List<MaintenanceRecord>()
            : _maint.GetLog(legacyBuilding)
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
        var oldestUnread = allNotifications
            .Where(notification => !notification.Read)
            .OrderBy(notification => notification.Ts, StringComparer.Ordinal)
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


}