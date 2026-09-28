using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// ترجمة حرفية لشكل بيانات الشهر (building.months[monthKey]) في Firestore
// وكل العناصر اللي جواه: مصروفات، إيرادات، دفعات، توزيعات.

[FirestoreData]
public class MonthData
{
    [FirestoreProperty("collections")]
    public Dictionary<string, object> Collections { get; set; } = new();

    [FirestoreProperty("expenses")]
    public List<Expense> Expenses { get; set; } = new();

    [FirestoreProperty("deposits")]
    public List<Deposit> Deposits { get; set; } = new();

    [FirestoreProperty("revenues")]
    public List<Revenue> Revenues { get; set; } = new();

    [FirestoreProperty("distribution")]
    public Dictionary<string, double> Distribution { get; set; } = new();

    [FirestoreProperty("revenueDistribution")]
    public Dictionary<string, double> RevenueDistribution { get; set; } = new();
}

[FirestoreData]
public class Expense
{
    [FirestoreProperty("id")] public string Id { get; set; } = "";
    [FirestoreProperty("categoryId")] public string CategoryId { get; set; } = "";
    [FirestoreProperty("note")] public string Note { get; set; } = "";
    [FirestoreProperty("amount")] public double Amount { get; set; }
    [FirestoreProperty("date")] public string Date { get; set; } = "";
    [FirestoreProperty("receipt")] public string? Receipt { get; set; }
}

[FirestoreData]
public class Revenue
{
    [FirestoreProperty("id")] public string Id { get; set; } = "";
    [FirestoreProperty("categoryId")] public string CategoryId { get; set; } = "";
    [FirestoreProperty("note")] public string Note { get; set; } = "";
    [FirestoreProperty("amount")] public double Amount { get; set; }
    [FirestoreProperty("date")] public string Date { get; set; } = "";
}

// حالة الدفعة: pending | confirmed | cancelled
[FirestoreData]
public class Deposit
{
    [FirestoreProperty("id")] public string Id { get; set; } = "";
    [FirestoreProperty("number")] public string Number { get; set; } = "";
    [FirestoreProperty("aptId")] public string AptId { get; set; } = "";
    [FirestoreProperty("amount")] public double Amount { get; set; }
    [FirestoreProperty("note")] public string Note { get; set; } = "";
    [FirestoreProperty("status")] public string Status { get; set; } = "pending";
    [FirestoreProperty("receipt")] public ReceiptData? Receipt { get; set; }
    [FirestoreProperty("createdAt")] public string CreatedAt { get; set; } = "";
    [FirestoreProperty("createdBy")] public string CreatedBy { get; set; } = "";
    [FirestoreProperty("confirmedAt")] public string? ConfirmedAt { get; set; }
    [FirestoreProperty("confirmedBy")] public string? ConfirmedBy { get; set; }
    [FirestoreProperty("cancelledAt")] public string? CancelledAt { get; set; }
    [FirestoreProperty("cancelledBy")] public string? CancelledBy { get; set; }
    [FirestoreProperty("cancelledReason")] public string? CancelledReason { get; set; }

    // ✅ Phase 21: تتبع تعديلات الأدمن على الدفعة المعلقة
    [FirestoreProperty("updatedAt")] public string? UpdatedAt { get; set; }
    [FirestoreProperty("updatedBy")] public string? UpdatedBy { get; set; }
    [FirestoreProperty("updateReason")] public string? UpdateReason { get; set; }
    [FirestoreProperty("editHistory")] public List<DepositEditEntry>? EditHistory { get; set; }
}

// ✅ Phase 21: سجل تعديل واحد على دفعة
[FirestoreData]
public class DepositEditEntry
{
    [FirestoreProperty("ts")] public string Ts { get; set; } = "";
    [FirestoreProperty("by")] public string By { get; set; } = "";
    [FirestoreProperty("reason")] public string Reason { get; set; } = "";
    [FirestoreProperty("oldAmount")] public double OldAmount { get; set; }
    [FirestoreProperty("newAmount")] public double NewAmount { get; set; }
    [FirestoreProperty("oldNote")] public string OldNote { get; set; } = "";
    [FirestoreProperty("newNote")] public string NewNote { get; set; } = "";
}

[FirestoreData]
public class WalletAdjustment
{
    [FirestoreProperty("id")] public string Id { get; set; } = "";
    [FirestoreProperty("aptId")] public string AptId { get; set; } = "";
    [FirestoreProperty("amount")] public double Amount { get; set; }
    [FirestoreProperty("reason")] public string Reason { get; set; } = "";
    [FirestoreProperty("monthKey")] public string MonthKey { get; set; } = "";
    [FirestoreProperty("createdAt")] public string CreatedAt { get; set; } = "";
    [FirestoreProperty("createdBy")] public string CreatedBy { get; set; } = "";
}

[FirestoreData]
public class AuditLogEntry
{
    [FirestoreProperty("id")] public string Id { get; set; } = "";
    [FirestoreProperty("ts")] public string Ts { get; set; } = "";
    [FirestoreProperty("action")] public string Action { get; set; } = "";
    [FirestoreProperty("label")] public string Label { get; set; } = "";
    [FirestoreProperty("actor")] public string Actor { get; set; } = "";
    [FirestoreProperty("actorRole")] public string ActorRole { get; set; } = "";
    [FirestoreProperty("details")] public string Details { get; set; } = "";
    [FirestoreProperty("month")] public string Month { get; set; } = "";
    [FirestoreProperty("buildingNumber")] public string? BuildingNumber { get; set; }
    [FirestoreProperty("aptNumber")] public int? AptNumber { get; set; }
}

public class WalletTransactionVm
{
    public string Type { get; set; } = "";
    public string Status { get; set; } = "confirmed";
    public string Id { get; set; } = "";
    public string? Number { get; set; }
    public double Amount { get; set; }
    public string Note { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

[FirestoreData]
public class ReceiptData
{
    [FirestoreProperty("url")] public string Url { get; set; } = "";
    [FirestoreProperty("path")] public string Path { get; set; } = "";
    [FirestoreProperty("fileName")] public string FileName { get; set; } = "";
    [FirestoreProperty("fileSize")] public long FileSize { get; set; }
    [FirestoreProperty("fileType")] public string FileType { get; set; } = "";
    [FirestoreProperty("success")] public bool Success { get; set; }
}