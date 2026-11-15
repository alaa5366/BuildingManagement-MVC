using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.ViewModels;

// ✅ Phase 24.2 — Building Map ViewModels
// بتجمع: Building + Presence + Balance + Status

/// <summary>
/// Container for the building map view models.
/// </summary>
public class BuildingMapViewModels
{
    /// <summary>
    /// Gets or sets the building.
    /// </summary>
    public Building Building { get; set; } = null!;
    /// <summary>
    /// Gets or sets the floors.
    /// </summary>
    public List<FloorMapVm> Floors { get; set; } = new();
    /// <summary>
    /// Gets or sets the stats.
    /// </summary>
    public MapStatsVm Stats { get; set; } = new();
    /// <summary>
    /// Gets or sets the current month.
    /// </summary>
    public string CurrentMonth { get; set; } = "";
}

/// <summary>
/// One floor with its apartment cards on the building map.
/// </summary>
public class FloorMapVm
{
    /// <summary>
    /// Gets or sets the identifier of the floor.
    /// </summary>
    public string FloorId { get; set; } = "";
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the order.
    /// </summary>
    public int Order { get; set; }
    /// <summary>
    /// Gets or sets the apartments.
    /// </summary>
    public List<ApartmentCardVm> Apartments { get; set; } = new();

    // إحصائيات الدور
    /// <summary>
    /// Gets the online count.
    /// </summary>
    public int OnlineCount => Apartments.Count(a => a.IsOnline);
    /// <summary>
    /// Gets the debtors count.
    /// </summary>
    public int DebtorsCount => Apartments.Count(a => a.Balance < 0);
}

/// <summary>
/// One apartment card on the building map.
/// </summary>
public class ApartmentCardVm
{
    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public Apartment Apt { get; set; } = null!;
    /// <summary>
    /// Gets or sets the balance.
    /// </summary>
    public decimal Balance { get; set; }
    /// <summary>
    /// Gets or sets the pending deposits count.
    /// </summary>
    public int PendingDepositsCount { get; set; }

    // Presence
    /// <summary>
    /// Gets or sets a value indicating whether it is online.
    /// </summary>
    public bool IsOnline { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is recent.
    /// </summary>
    public bool IsRecent { get; set; }
    /// <summary>
    /// Gets or sets the last seen text.
    /// </summary>
    public string LastSeenText { get; set; } = Loc.T("Never_Logged_In");

    // Status — للألوان والأيقونات
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "offline";   // online | recent | offline | negative | closed
    /// <summary>
    /// Gets or sets the status color.
    /// </summary>
    public string StatusColor { get; set; } = "#999";
    /// <summary>
    /// Gets or sets the status icon.
    /// </summary>
    public string StatusIcon { get; set; } = "⚪";
    /// <summary>
    /// Gets or sets the status label.
    /// </summary>
    public string StatusLabel { get; set; } = "";
    /// <summary>
    /// Gets a value indicating whether it is disabled.
    /// </summary>
    public bool IsDisabled => Apt.Disabled;
    /// <summary>
    /// Gets a value indicating whether it is closed.
    /// </summary>
    public bool IsClosed => Apt.Closed;
    // Helpers
    /// <summary>
    /// Gets the owner display.
    /// </summary>
    public string OwnerDisplay => string.IsNullOrWhiteSpace(Apt.Owner) ? "—" : Apt.Owner;
    /// <summary>
    /// Gets the balance class.
    /// </summary>
    public string BalanceClass => Balance < 0 ? "text-danger" : "text-success";
}

/// <summary>
/// Summary statistics shown above the building map.
/// </summary>
public class MapStatsVm
{
    /// <summary>
    /// Gets or sets the floors count.
    /// </summary>
    public int FloorsCount { get; set; }
    /// <summary>
    /// Gets or sets the apartments count.
    /// </summary>
    public int AptsCount { get; set; }
    /// <summary>
    /// Gets or sets the open apartments count.
    /// </summary>
    public int OpenAptsCount { get; set; }
    /// <summary>
    /// Gets or sets the closed apartments count.
    /// </summary>
    public int ClosedAptsCount { get; set; }
    /// <summary>
    /// Gets or sets the online count.
    /// </summary>
    public int OnlineCount { get; set; }
    /// <summary>
    /// Gets or sets the total balance.
    /// </summary>
    public decimal TotalBalance { get; set; }
    /// <summary>
    /// Gets or sets the debtors count.
    /// </summary>
    public int DebtorsCount { get; set; }
    /// <summary>
    /// Gets or sets the creditors count.
    /// </summary>
    public int CreditorsCount { get; set; }
    /// <summary>
    /// Gets or sets the pending deposits count.
    /// </summary>
    public int PendingDepositsCount { get; set; }
}

// ============================================================
// ✅ Phase 24.3 — Apartment Details Modal
// ============================================================
/// <summary>
/// Details of one apartment shown in the details modal.
/// </summary>
public class ApartmentDetailsVm
{
    // بيانات أساسية
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string AptId { get; set; } = "";
    /// <summary>
    /// Gets or sets the apartment number.
    /// </summary>
    public int AptNumber { get; set; }
    /// <summary>
    /// Gets or sets the floor label.
    /// </summary>
    public string FloorLabel { get; set; } = "";
    /// <summary>
    /// Gets or sets the floor order.
    /// </summary>
    public int FloorOrder { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is closed.
    /// </summary>
    public bool IsClosed { get; set; }
    /// <summary>
    /// Gets or sets the close date text.
    /// </summary>
    public string CloseDateText { get; set; } = "—";  
    /// <summary>
    /// Gets or sets the apartment label.
    /// </summary>
    public string? AptLabel { get; set; }
    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    public string? Notes { get; set; }

    // بيانات الساكن
    /// <summary>
    /// Gets or sets the owner.
    /// </summary>
    public string Owner { get; set; } = "";
    /// <summary>
    /// Gets or sets the phone.
    /// </summary>
    public string Phone { get; set; } = "";
    /// <summary>
    /// Gets or sets the PIN.
    /// </summary>
    public string Pin { get; set; } = "";
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// Gets the owner initial.
    /// </summary>
    public string OwnerInitial => string.IsNullOrWhiteSpace(Owner) ? "?" : Owner.Trim()[0].ToString();
    /// <summary>
    /// Gets or sets a value indicating whether it is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }
    /// <summary>
    /// Gets or sets the disabled reason.
    /// </summary>
    public string DisabledReason { get; set; } = "";
    /// <summary>
    /// Gets or sets the disabled at text.
    /// </summary>
    public string DisabledAtText { get; set; } = "";
    // Financial
    /// <summary>
    /// Gets or sets the balance.
    /// </summary>
    public decimal Balance { get; set; }
    /// <summary>
    /// Gets or sets the monthly fee.
    /// </summary>
    public decimal MonthlyFee { get; set; }
    /// <summary>
    /// Gets or sets the pending deposits count.
    /// </summary>
    public int PendingDepositsCount { get; set; }
    /// <summary>
    /// Gets or sets the confirmed deposits count.
    /// </summary>
    public int ConfirmedDepositsCount { get; set; }
    /// <summary>
    /// Gets or sets the total collected this month.
    /// </summary>
    public decimal TotalCollectedThisMonth { get; set; }

    // Presence
    /// <summary>
    /// Gets or sets a value indicating whether it is online.
    /// </summary>
    public bool IsOnline { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is recent.
    /// </summary>
    public bool IsRecent { get; set; }
    /// <summary>
    /// Gets or sets the last seen text.
    /// </summary>
    public string LastSeenText { get; set; } = Loc.T("Never_Logged_In");
    /// <summary>
    /// Gets or sets the status icon.
    /// </summary>
    public string StatusIcon { get; set; } = "⚪";
    /// <summary>
    /// Gets or sets the status label.
    /// </summary>
    public string StatusLabel { get; set; } = "";
    /// <summary>
    /// Gets or sets the status color.
    /// </summary>
    public string StatusColor { get; set; } = "#999";

    // آخر 3 دفعات
    /// <summary>
    /// Gets or sets the recent deposits.
    /// </summary>
    public List<RecentDepositVm> RecentDeposits { get; set; } = new();

    // Building info (للروابط)
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the building name.
    /// </summary>
    public string BuildingName { get; set; } = "";
    /// <summary>
    /// Gets or sets the building number.
    /// </summary>
    public string BuildingNumber { get; set; } = "";
    /// <summary>
    /// Gets or sets the current month.
    /// </summary>
    public string CurrentMonth { get; set; } = "";
}

/// <summary>
/// A recent deposit shown in the apartment details.
/// </summary>
public class RecentDepositVm
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the number.
    /// </summary>
    public string Number { get; set; } = "";
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "";
    /// <summary>
    /// Gets or sets the date text.
    /// </summary>
    public string DateText { get; set; } = "";
    /// <summary>
    /// Gets or sets the note.
    /// </summary>
    public string Note { get; set; } = "";

    /// <summary>
    /// Gets the status label.
    /// </summary>
    public string StatusLabel => Status switch
    {
        "confirmed" => Loc.T("Confirmed"),
        "pending" => Loc.T("Pending"),
        "cancelled" => Loc.T("Cancelled"),
        _ => Status
    };
}