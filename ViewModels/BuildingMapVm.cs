using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.ViewModels;

// ✅ Phase 24.2 — Building Map ViewModels
// بتجمع: Building + Presence + Balance + Status

public class BuildingMapVm
{
    public Building Building { get; set; } = null!;
    public List<FloorMapVm> Floors { get; set; } = new();
    public MapStatsVm Stats { get; set; } = new();
    public string CurrentMonth { get; set; } = "";
}

public class FloorMapVm
{
    public string FloorId { get; set; } = "";
    public string Label { get; set; } = "";
    public int Order { get; set; }
    public List<ApartmentCardVm> Apartments { get; set; } = new();

    // إحصائيات الدور
    public int OnlineCount => Apartments.Count(a => a.IsOnline);
    public int DebtorsCount => Apartments.Count(a => a.Balance < 0);
}

public class ApartmentCardVm
{
    public Apartment Apt { get; set; } = null!;
    public double Balance { get; set; }
    public int PendingDepositsCount { get; set; }

    // Presence
    public bool IsOnline { get; set; }
    public bool IsRecent { get; set; }
    public string LastSeenText { get; set; } = Loc.T("Never_Logged_In");

    // Status — للألوان والأيقونات
    public string Status { get; set; } = "offline";   // online | recent | offline | negative | closed
    public string StatusColor { get; set; } = "#999";
    public string StatusIcon { get; set; } = "⚪";
    public string StatusLabel { get; set; } = "";

    // Helpers
    public string OwnerDisplay => string.IsNullOrWhiteSpace(Apt.Owner) ? "—" : Apt.Owner;
    public string BalanceClass => Balance < 0 ? "text-danger" : "text-success";
}

public class MapStatsVm
{
    public int FloorsCount { get; set; }
    public int AptsCount { get; set; }
    public int OpenAptsCount { get; set; }
    public int ClosedAptsCount { get; set; }
    public int OnlineCount { get; set; }
    public double TotalBalance { get; set; }
    public int DebtorsCount { get; set; }
    public int CreditorsCount { get; set; }
    public int PendingDepositsCount { get; set; }
}

// ============================================================
// ✅ Phase 24.3 — Apartment Details Modal
// ============================================================
public class ApartmentDetailsVm
{
    // بيانات أساسية
    public string AptId { get; set; } = "";
    public int AptNumber { get; set; }
    public string FloorLabel { get; set; } = "";
    public int FloorOrder { get; set; }
    public bool IsClosed { get; set; }
    public string CloseDateText { get; set; } = "—";  
    public string? AptLabel { get; set; }
    public string? Notes { get; set; }

    // بيانات الساكن
    public string Owner { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Pin { get; set; } = "";
    public string? Email { get; set; }
    public string OwnerInitial => string.IsNullOrWhiteSpace(Owner) ? "?" : Owner.Trim()[0].ToString();

    // Financial
    public double Balance { get; set; }
    public double MonthlyFee { get; set; }
    public int PendingDepositsCount { get; set; }
    public int ConfirmedDepositsCount { get; set; }
    public double TotalCollectedThisMonth { get; set; }

    // Presence
    public bool IsOnline { get; set; }
    public bool IsRecent { get; set; }
    public string LastSeenText { get; set; } = Loc.T("Never_Logged_In");
    public string StatusIcon { get; set; } = "⚪";
    public string StatusLabel { get; set; } = "";
    public string StatusColor { get; set; } = "#999";

    // آخر 3 دفعات
    public List<RecentDepositVm> RecentDeposits { get; set; } = new();

    // Building info (للروابط)
    public string BuildingId { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public string BuildingNumber { get; set; } = "";
    public string CurrentMonth { get; set; } = "";
}

public class RecentDepositVm
{
    public string Id { get; set; } = "";
    public string Number { get; set; } = "";
    public double Amount { get; set; }
    public string Status { get; set; } = "";
    public string DateText { get; set; } = "";
    public string Note { get; set; } = "";

    public string StatusLabel => Status switch
    {
        "confirmed" => Loc.T("Confirmed"),
        "pending" => Loc.T("Pending"),
        "cancelled" => Loc.T("Cancelled"),
        _ => Status
    };
}