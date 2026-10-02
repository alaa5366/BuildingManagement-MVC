using BuildingManagementMvc.Models;
using BuildingManagementMvc.ViewModels;

namespace BuildingManagementMvc.Services;

// ✅ Phase 24.2 — Building Map Service
public class BuildingMapService
{
    private readonly WalletService _wallet;
    private readonly PresenceService _presence;

    public BuildingMapService(WalletService wallet, PresenceService presence)
    {
        _wallet = wallet;
        _presence = presence;
    }

    public async Task<BuildingMapVm> BuildAsync(Building building, string currentMonth)
    {
        var vm = new BuildingMapVm
        {
            Building = building,
            CurrentMonth = currentMonth
        };

        // 1. جيب الـ Presence للعمارة
        var presenceMap = await _presence.GetByBuildingAsync(building.Id);

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
                var card = BuildApartmentCard(building, apt, currentMonth, presenceMap);

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
        Building building,
        Apartment apt,
        string currentMonth,
        Dictionary<string, Presence> presenceMap)
    {
        // الرصيد
        var balance = _wallet.ComputeWalletBalance(building, apt.Id, currentMonth);

        // الدفعات المعلقة
        var pending = 0;
        if (building.Months.TryGetValue(currentMonth, out var m))
        {
            pending = m.Deposits.Count(d => d.AptId == apt.Id && d.Status == "pending");
        }

        var card = new ApartmentCardVm
        {
            Apt = apt,
            Balance = balance,
            PendingDepositsCount = pending
        };

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

        // 4. آخر ظهور قريب (< 24 ساعة) → أصفر
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
            AptLabel = apt.Label,
            Notes = apt.Notes,

            Owner = apt.Owner,
            Phone = apt.Phone,
            Pin = apt.Pin,
            Email = apt.Email,

            MonthlyFee = apt.MonthlyFee,

            BuildingId = building.Id,
            BuildingName = building.Name,
            BuildingNumber = building.BuildingNumber,
            CurrentMonth = currentMonth
        };

        // الرصيد
        vm.Balance = _wallet.ComputeWalletBalance(building, apt.Id, currentMonth);

        // الدفعات
        if (building.Months.TryGetValue(currentMonth, out var m))
        {
            var aptDeposits = m.Deposits.Where(d => d.AptId == apt.Id).ToList();

            vm.PendingDepositsCount = aptDeposits.Count(d => d.Status == "pending");
            vm.ConfirmedDepositsCount = aptDeposits.Count(d => d.Status == "confirmed");
            vm.TotalCollectedThisMonth = aptDeposits
                .Where(d => d.Status == "confirmed")
                .Sum(d => d.Amount);

            // آخر 3 دفعات
            vm.RecentDeposits = aptDeposits
                .OrderByDescending(d => d.CreatedAt)
                .Take(3)
                .Select(d => new RecentDepositVm
                {
                    Id = d.Id,
                    Number = d.Number,
                    Amount = d.Amount,
                    Status = d.Status,
                    Note = d.Note,
                    DateText = FormatDate(d.CreatedAt)
                })
                .ToList();
        }

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