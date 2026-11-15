namespace BuildingManagementMvc.Models;

// ============================================================
// Building — aggregate كامل للعمارة (مستخدم في كل الخدمات)
// كان Firestore doc، دلوقتي SQL (عن طريق BuildingSqlStore)
// ============================================================

/// <summary>
/// Aggregate root of the domain: floors, apartments, categories, monthly financial data, wallet adjustments and the audit trail of one building.
/// </summary>
public class Building
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the building number.
    /// </summary>
    public string BuildingNumber { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the admin PIN.
    /// </summary>
    public string AdminPin { get; set; } = "";
    /// <summary>
    /// Gets or sets the data version.
    /// </summary>
    public string DataVersion { get; set; } = "3.2";
    /// <summary>
    /// Gets or sets the logo URL.
    /// </summary>
    public string LogoUrl { get; set; } = "";
    /// <summary>
    /// Gets or sets the admin WhatsApp.
    /// </summary>
    public string AdminWhatsapp { get; set; } = "";
    /// <summary>Concurrency token loaded from the SQL building row.</summary>
    public byte[]? RowVersion { get; set; }
    public int ApartmentsCount { get; set; }
    public int FloorsCount { get; set; }

    /// <summary>
    /// Gets or sets the floors.
    /// </summary>
    public List<Floor> Floors { get; set; } = new();
    /// <summary>
    /// Gets or sets the apartments.
    /// </summary>
    public List<Apartment> Apartments { get; set; } = new();
    /// <summary>
    /// Gets or sets the months.
    /// </summary>
    public Dictionary<string, MonthData> Months { get; set; } = new();
    /// <summary>
    /// Gets or sets the wallet transactions.
    /// </summary>
    public List<Dictionary<string, object>> WalletTransactions { get; set; } = new();
    /// <summary>
    /// Gets or sets the wallet adjustments.
    /// </summary>
    public List<WalletAdjustment> WalletAdjustments { get; set; } = new();
    /// <summary>
    /// Gets or sets the expense categories.
    /// </summary>
    public List<FinancialCategory> ExpenseCategories { get; set; } = new();
    /// <summary>
    /// Gets or sets the revenue categories.
    /// </summary>
    public List<FinancialCategory> RevenueCategories { get; set; } = new();
    /// <summary>
    /// Gets or sets the payment information.
    /// </summary>
    public PaymentInfo PaymentInfo { get; set; } = new();
    /// <summary>
    /// Gets or sets the audit log.
    /// </summary>
    public List<AuditLogEntry> AuditLog { get; set; } = new();
    /// <summary>
    /// Gets or sets the admin uids.
    /// </summary>
    public List<string> AdminUids { get; set; } = new();
    /// <summary>
    /// Gets or sets the notifications.
    /// </summary>
    public List<Notification> Notifications { get; set; } = new();
    /// <summary>
    /// Gets or sets the polls.
    /// </summary>
    public List<Poll> Polls { get; set; } = new();
    /// <summary>
    /// Gets or sets the maintenance log.
    /// </summary>
    public List<MaintenanceRecord> MaintenanceLog { get; set; } = new();
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string CreatedAt { get; set; } = "";
}

/// <summary>
/// A floor of a building.
/// </summary>
public class Floor
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the order.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// An apartment: owner, contact data, monthly fee and open/closed status.
/// </summary>
public class Apartment
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the floor.
    /// </summary>
    public string FloorId { get; set; } = "";
    /// <summary>
    /// Gets or sets the number.
    /// </summary>
    public int Number { get; set; }
    /// <summary>
    /// Gets or sets the owner.
    /// </summary>
    public string Owner { get; set; } = "";
    /// <summary>
    /// Gets or sets the phone.
    /// </summary>
    public string Phone { get; set; } = "";
    /// <summary>
    /// Gets or sets the monthly fee.
    /// </summary>
    public decimal MonthlyFee { get; set; }
    /// <summary>
    /// Gets or sets the PIN.
    /// </summary>
    public string Pin { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is closed.
    /// </summary>
    public bool Closed { get; set; }
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = "";
    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    public string Notes { get; set; } = "";
    /// <summary>
    /// Gets or sets the open date.
    /// </summary>
    public string OpenDate { get; set; } = "";
    /// <summary>
    /// Gets or sets the close date.
    /// </summary>
    public string? CloseDate { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is disabled.
    /// </summary>
    public bool Disabled { get; set; }
    /// <summary>
    /// Gets or sets the disabled reason.
    /// </summary>
    public string DisabledReason { get; set; } = "";
    /// <summary>
    /// Gets or sets the settings.
    /// </summary>
    public UserSettingsDoc? Settings { get; set; }
}

/// <summary>
/// An expense or revenue category.
/// </summary>
public class FinancialCategory
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    public string Color { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is active.
    /// </summary>
    public bool Active { get; set; } = true;
    /// <summary>
    /// Gets or sets the order.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Transfer details (bank account, wallet number, ...) shown to residents and printed on invoices.
/// </summary>
public class PaymentInfo
{
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the account number.
    /// </summary>
    public string AccountNumber { get; set; } = "";
    /// <summary>
    /// Gets or sets the phone.
    /// </summary>
    public string Phone { get; set; } = "";
    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    public string Notes { get; set; } = "";
}