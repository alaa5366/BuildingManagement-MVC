namespace BuildingManagementMvc.Models;

// ============================================================
// Building — aggregate كامل للعمارة (مستخدم في كل الخدمات)
// كان Firestore doc، دلوقتي SQL (عن طريق BuildingSqlStore)
// ============================================================

public class Building
{
    public string Id { get; set; } = "";
    public string BuildingNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string AdminPin { get; set; } = "";
    public string DataVersion { get; set; } = "3.2";
    public string LogoUrl { get; set; } = "";
    public string AdminWhatsapp { get; set; } = "";

    public List<Floor> Floors { get; set; } = new();
    public List<Apartment> Apartments { get; set; } = new();
    public Dictionary<string, MonthData> Months { get; set; } = new();
    public List<Dictionary<string, object>> WalletTransactions { get; set; } = new();
    public List<WalletAdjustment> WalletAdjustments { get; set; } = new();
    public List<FinancialCategory> ExpenseCategories { get; set; } = new();
    public List<FinancialCategory> RevenueCategories { get; set; } = new();
    public PaymentInfo PaymentInfo { get; set; } = new();
    public List<AuditLogEntry> AuditLog { get; set; } = new();
    public List<string> AdminUids { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
    public List<Poll> Polls { get; set; } = new();
    public List<MaintenanceRecord> MaintenanceLog { get; set; } = new();
    public string CreatedAt { get; set; } = "";
}

public class Floor
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public int Order { get; set; }
}

public class Apartment
{
    public string Id { get; set; } = "";
    public string FloorId { get; set; } = "";
    public int Number { get; set; }
    public string Owner { get; set; } = "";
    public string Phone { get; set; } = "";
    public double MonthlyFee { get; set; }
    public string Pin { get; set; } = "";
    public bool Closed { get; set; }
    public string Label { get; set; } = "";
    public string Email { get; set; } = "";
    public string Notes { get; set; } = "";
    public string OpenDate { get; set; } = "";
    public string? CloseDate { get; set; }
    public bool Disabled { get; set; }
    public string DisabledReason { get; set; } = "";
    public UserSettingsDoc? Settings { get; set; }
}

public class FinancialCategory
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public bool Active { get; set; } = true;
    public int Order { get; set; }
}

public class PaymentInfo
{
    public string Label { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Notes { get; set; } = "";
}