using BuildingManagementMvc.Models;
using BuildingManagementMvc.ViewModels;

namespace BuildingManagementMvc.Services;

// ✅ Phase 24.2 — Building Map Service
/// <summary>
/// Builds the building map view models (floors, apartment cards, statistics and apartment details).
/// </summary>
public class BuildingMapService
{
    private readonly TargetWalletReadService _targetWallet;
    private readonly PresenceService _presence;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildingMapService"/> class.
    /// </summary>
    public BuildingMapService(TargetWalletReadService targetWallet, PresenceService presence)
    {
        _targetWallet = targetWallet;
        _presence = presence;
    }

    /// <summary>
    /// Builds the operation on the building map.
    /// </summary>
    public async Task<BuildingMapViewModels> BuildAsync(Building building, string currentMonth)
    {
        var vm = new BuildingMapViewModels
        {
            Building = building,
            CurrentMonth = currentMonth
        };

        // 1. جيب الـ Presence للعمارة
        var presenceMap = await _presence.GetByBuildingAsync(building.Id);
        var walletSnapshot = await _targetWallet.GetAdminSnapshotAsync(
            building.Id, currentMonth);
        var pendingCounts = walletSnapshot.PendingDeposits
            .Where(item => item.MonthKey == currentMonth)
            .GroupBy(item => item.ApartmentId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        // 2. إحصائيات عامة
        var stats = new MapStatsVm
        {
            FloorsCount = building.Floors.Count,
            AptsCount = building.Apartments.Count,
            OpenAptsCount = building.Apartments.Count(a => !a.Closed),
            ClosedAptsCount = building.Apartments.Count(a => a.Closed)
        };

        // 3. لكل دور
        foreach (var floor in building.Floors.OrderBy(f => f.Order))
        {
            var floorVm = new FloorMapVm
            {
                FloorId = floor.Id,
                Label = floor.Label,
                Order = floor.Order
            };

            var apts = building.Apartments
                .Where(a => a.FloorId == floor.Id)
                .OrderBy(a => a.Number);

            foreach (var apt in apts)
            {
                var card = BuildApartmentCard(
                    apt, presenceMap,
                    walletSnapshot.Balances.GetValueOrDefault(apt.Id),
                    pendingCounts.GetValueOrDefault(apt.Id));

                floorVm.Apartments.Add(card);

                // إحصائيات
                stats.TotalBalance += card.Balance;
                stats.PendingDepositsCount += card.PendingDepositsCount;
                if (card.IsOnline) stats.OnlineCount++;
                if (card.Balance < 0) stats.DebtorsCount++;
                else if (card.Balance > 0) stats.CreditorsCount++;
            }

            vm.Floors.Add(floorVm);
        }

        vm.Stats = stats;
        return vm;
    }

    // ============================================================
    // Helper: بناء بطاقة شقة
    // ============================================================
    private ApartmentCardVm BuildApartmentCard(
        Apartment apt,
        Dictionary<string, Presence> presenceMap,
        decimal balance,
        int pending)
    {

        var card = new ApartmentCardVm
        {
            Apt = apt,
            Balance = balance,
            PendingDepositsCount = pending
        };

        // ✅ Disabled flag
        card.Apt = apt;
        // Presence
        if (presenceMap.TryGetValue(apt.Id, out var presence))
        {
            card.IsOnline = PresenceService.IsOnline(presence);
            card.IsRecent = PresenceService.IsRecent(presence);
            card.LastSeenText = PresenceService.TimeAgo(presence);
        }

        // الحالة (اللون)
        DetermineStatus(card);
        return card;
    }

    // ============================================================
    // Helper: تحديد حالة الشقة
    // ============================================================
    private static void DetermineStatus(ApartmentCardVm card)
    {
        // 0. معطلة → بنفسجي
        if (card.Apt.Disabled)
        {
            card.Status = "disabled";
            card.StatusColor = "#8E6BB2";
            card.StatusIcon = "🚫";
            card.StatusLabel = Loc.T("Disabled_3");
            return;
        }

        // 1. مغلقة → أزرق
        if (card.Apt.Closed)
        {
            card.Status = "closed";
            card.StatusColor = "#5B9BD5";
            card.StatusIcon = "🔒";
            card.StatusLabel = Loc.T("Closed");
            return;
        }

        // 2. أونلاين → أخضر
        if (card.IsOnline)
        {
            card.Status = "online";
            card.StatusColor = "#2E7D5B";
            card.StatusIcon = "🟢";
            card.StatusLabel = Loc.T("Online");
            return;
        }

        // 3. رصيد سالب → أحمر
        if (card.Balance < 0)
        {
            card.Status = "negative";
            card.StatusColor = "#A0432A";
            card.StatusIcon = "🔴";
            card.StatusLabel = Loc.T("Indebted");
            return;
        }

        // 4. آخر ظهور قريب → أصفر
        if (card.IsRecent)
        {
            card.Status = "recent";
            card.StatusColor = "#D9B34A";
            card.StatusIcon = "🟡";
            card.StatusLabel = Loc.T("Seen_Recently");
            return;
        }

        // 5. أوفلاين → رمادي
        card.Status = "offline";
        card.StatusColor = "#999";
        card.StatusIcon = "⚪";
        card.StatusLabel = Loc.T("Offline");
    }

    // ============================================================
    // ✅ Phase 24.3 — Apartment Details
    // ============================================================
    /// <summary>
    /// Gets the apartment details.
    /// </summary>
    public async Task<ApartmentDetailsVm?> GetApartmentDetailsAsync(
        Building building,
        string apartmentId,
        string currentMonth)
    {
        var apt = building.Apartments.FirstOrDefault(a => a.Id == apartmentId);
        if (apt == null) return null;

        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);

        var vm = new ApartmentDetailsVm
        {
            AptId = apt.Id,
            AptNumber = apt.Number,
            FloorLabel = floor?.Label ?? "—",
            FloorOrder = floor?.Order ?? 0,
            IsClosed = apt.Closed,
            IsDisabled = apt.Disabled,
            DisabledReason = apt.DisabledReason ?? "",
            AptLabel = apt.Label,
            Notes = apt.Notes,

            Owner = apt.Owner,
            Phone = apt.Phone,
            Pin = "",
            Email = apt.Email,

            MonthlyFee = apt.MonthlyFee,

            BuildingId = building.Id,
            BuildingName = building.Name,
            BuildingNumber = building.BuildingNumber,
            CurrentMonth = currentMonth
        };

        // Financial values and deposit history come from the canonical ledger.
        var walletSnapshot = await _targetWallet.GetApartmentAsync(
            building.Id, apt.Id, currentMonth, currentMonth);
        vm.Balance = walletSnapshot.Balance;

        var monthDeposits = walletSnapshot.Transactions
            .Where(item => item.Type == "deposit")
            .ToList();
        var pendingDeposits = walletSnapshot.PendingDeposits;
        vm.PendingDepositsCount = pendingDeposits.Count;
        vm.ConfirmedDepositsCount = monthDeposits.Count(item => item.Status == "confirmed");
        vm.TotalCollectedThisMonth = monthDeposits
            .Where(item => item.Status == "confirmed")
            .Sum(item => item.Amount);

        vm.RecentDeposits = monthDeposits
            .Select(item => new
            {
                item.CreatedAt,
                Deposit = new RecentDepositVm
                {
                    Id = item.Id,
                    Number = item.Number ?? string.Empty,
                    Amount = item.Amount,
                    Status = item.Status,
                    Note = item.Note ?? string.Empty,
                    DateText = FormatDate(item.CreatedAt)
                }
            })
            .Concat(pendingDeposits.Select(item => new
            {
                CreatedAt = item.CreatedAt,
                Deposit = new RecentDepositVm
                {
                    Id = item.Id,
                    Number = item.Number,
                    Amount = item.Amount,
                    Status = item.Status,
                    Note = item.Note ?? string.Empty,
                    DateText = FormatDate(item.CreatedAt)
                }
            }))
            .OrderByDescending(item => item.CreatedAt, StringComparer.Ordinal)
            .Take(3)
            .Select(item => item.Deposit)
            .ToList();

        // Presence
        var presenceMap = await _presence.GetByBuildingAsync(building.Id);
        if (presenceMap.TryGetValue(apt.Id, out var presence))
        {
            vm.IsOnline = PresenceService.IsOnline(presence);
            vm.IsRecent = PresenceService.IsRecent(presence);
            vm.LastSeenText = PresenceService.TimeAgo(presence);
        }

        // الحالة
        DetermineStatusForDetails(vm);

        return vm;
    }

    private static void DetermineStatusForDetails(ApartmentDetailsVm vm)
    {
        // 0. معطلة → بنفسجي
        if (vm.IsDisabled)
        {
            vm.StatusIcon = "🚫"; vm.StatusLabel = Loc.T("Disabled_3");
            vm.StatusColor = "#8E6BB2"; return;
        }

        if (vm.IsClosed)
        {
            vm.StatusIcon = "🔵"; vm.StatusLabel = Loc.T("Closed");
            vm.StatusColor = "#5B9BD5"; return;
        }
        if (vm.IsOnline)
        {
            vm.StatusIcon = "🟢"; vm.StatusLabel = Loc.T("Online");
            vm.StatusColor = "#2E7D5B"; return;
        }
        if (vm.Balance < 0)
        {
            vm.StatusIcon = "🔴"; vm.StatusLabel = Loc.T("Indebted");
            vm.StatusColor = "#A0432A"; return;
        }
        if (vm.IsRecent)
        {
            vm.StatusIcon = "🟡"; vm.StatusLabel = Loc.T("Seen_Recently");
            vm.StatusColor = "#D9B34A"; return;
        }
        vm.StatusIcon = "⚪"; vm.StatusLabel = Loc.T("Offline");
        vm.StatusColor = "#999";
    }

    private static string FormatDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return "—";
        if (DateTime.TryParse(iso, out var dt))
        {
            var diff = DateTime.UtcNow - dt.ToUniversalTime();
            if (diff.TotalMinutes < 60) return Loc.T("N_Minutes_Ago", (int)diff.TotalMinutes);
            if (diff.TotalHours < 24) return Loc.T("N_Hours_Ago", (int)diff.TotalHours);
            if (diff.TotalDays < 7) return Loc.T("N_Days_Ago", (int)diff.TotalDays);
            return dt.ToString("yyyy-MM-dd");
        }
        return "—";
    }
}