using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// ============================================================
// نفس شكل مستند "buildings" في Firestore بالظبط، عشان تشتغل
// على نفس قاعدة البيانات المستخدمة في نسخة الـ JS من غير أي تعديل
// أو ترحيل بيانات.
// ============================================================

[FirestoreData]
public class Building
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "";

    [FirestoreProperty("buildingNumber")]
    public string BuildingNumber { get; set; } = "";

    [FirestoreProperty("name")]
    public string Name { get; set; } = "";

    [FirestoreProperty("adminPin")]
    public string AdminPin { get; set; } = "";

    [FirestoreProperty("dataVersion")]
    public string DataVersion { get; set; } = "3.2";

    [FirestoreProperty("logoUrl")]
    public string LogoUrl { get; set; } = "";

    [FirestoreProperty("adminWhatsapp")]
    public string AdminWhatsapp { get; set; } = "";

    [FirestoreProperty("floors")]
    public List<Floor> Floors { get; set; } = new();

    [FirestoreProperty("apartments")]
    public List<Apartment> Apartments { get; set; } = new();

    [FirestoreProperty("months")]
    public Dictionary<string, MonthData> Months { get; set; } = new();

    [FirestoreProperty("walletTransactions")]
    public List<Dictionary<string, object>> WalletTransactions { get; set; } = new();

    [FirestoreProperty("walletAdjustments")]
    public List<WalletAdjustment> WalletAdjustments { get; set; } = new();

    [FirestoreProperty("expenseCategories")]
    public List<FinancialCategory> ExpenseCategories { get; set; } = new();

    [FirestoreProperty("revenueCategories")]
    public List<FinancialCategory> RevenueCategories { get; set; } = new();

    [FirestoreProperty("paymentInfo")]
    public PaymentInfo PaymentInfo { get; set; } = new();

    [FirestoreProperty("auditLog")]
    public List<AuditLogEntry> AuditLog { get; set; } = new();

    [FirestoreProperty("adminUids")]
    public List<string> AdminUids { get; set; } = new();

    [FirestoreProperty("notifications")]
    public List<Dictionary<string, object>> Notifications { get; set; } = new();

    [FirestoreProperty("polls")]
    public List<Dictionary<string, object>> Polls { get; set; } = new();

    [FirestoreProperty("maintenanceLog")]
    public List<Dictionary<string, object>> MaintenanceLog { get; set; } = new();

    [FirestoreProperty("createdAt")]
    public string CreatedAt { get; set; } = "";
}

[FirestoreData]
public class Floor
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = "";

    [FirestoreProperty("label")]
    public string Label { get; set; } = "";

    [FirestoreProperty("order")]
    public int Order { get; set; }
}

[FirestoreData]
public class Apartment
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = "";

    [FirestoreProperty("floorId")]
    public string FloorId { get; set; } = "";

    [FirestoreProperty("number")]
    public int Number { get; set; }

    [FirestoreProperty("owner")]
    public string Owner { get; set; } = "";

    [FirestoreProperty("phone")]
    public string Phone { get; set; } = "";

    [FirestoreProperty("monthlyFee")]
    public double MonthlyFee { get; set; }

    [FirestoreProperty("pin")]
    public string Pin { get; set; } = "";

    [FirestoreProperty("closed")]
    public bool Closed { get; set; }

    [FirestoreProperty("label")]
    public string Label { get; set; } = "";

    [FirestoreProperty("email")]
    public string Email { get; set; } = "";

    [FirestoreProperty("notes")]
    public string Notes { get; set; } = "";

    [FirestoreProperty("openDate")]
    public string OpenDate { get; set; } = "";

    [FirestoreProperty("closeDate")]
    public string? CloseDate { get; set; }

    [FirestoreProperty("disabled")]
    public bool Disabled { get; set; }

    [FirestoreProperty("disabledReason")]
    public string DisabledReason { get; set; } = "";
}

[FirestoreData]
public class FinancialCategory
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = "";

    [FirestoreProperty("name")]
    public string Name { get; set; } = "";

    [FirestoreProperty("color")]
    public string Color { get; set; } = "";

    [FirestoreProperty("active")]
    public bool Active { get; set; } = true;

    [FirestoreProperty("order")]
    public int Order { get; set; }
}

[FirestoreData]
public class PaymentInfo
{
    [FirestoreProperty("label")]
    public string Label { get; set; } = "";

    [FirestoreProperty("accountNumber")]
    public string AccountNumber { get; set; } = "";

    [FirestoreProperty("phone")]
    public string Phone { get; set; } = "";

    [FirestoreProperty("notes")]
    public string Notes { get; set; } = "";
}
