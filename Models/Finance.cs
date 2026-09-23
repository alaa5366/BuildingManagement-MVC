using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// ترجمة حرفية لشكل بيانات الشهر (building.months[monthKey]) في Firestore
// وكل العناصر اللي جواه: مصروفات، إيرادات، دفعات، توزيعات.

[FirestoreData]
public class MonthData
{
    // نظام قديم (Legacy) — تحصيل شقة واحدة لكل شهر، لسه موجود لأغراض التوافق
    [FirestoreProperty("collections")]
    public Dictionary<string, object> Collections { get; set; } = new();

    [FirestoreProperty("expenses")]
    public List<Expense> Expenses { get; set; } = new();

    [FirestoreProperty("deposits")]
    public List<Deposit> Deposits { get; set; } = new();

    [FirestoreProperty("revenues")]
    public List<Revenue> Revenues { get; set; } = new();

    // aptId -> نصيبه من مصروفات الشهر
    [FirestoreProperty("distribution")]
    public Dictionary<string, double> Distribution { get; set; } = new();

    // aptId -> نصيبه من إيرادات الشهر
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
    [FirestoreProperty("createdAt")] public string CreatedAt { get; set; } = "";
    [FirestoreProperty("createdBy")] public string CreatedBy { get; set; } = "";
    [FirestoreProperty("confirmedAt")] public string? ConfirmedAt { get; set; }
    [FirestoreProperty("confirmedBy")] public string? ConfirmedBy { get; set; }
    [FirestoreProperty("cancelledAt")] public string? CancelledAt { get; set; }
    [FirestoreProperty("cancelledBy")] public string? CancelledBy { get; set; }
    [FirestoreProperty("cancelledReason")] public string? CancelledReason { get; set; }
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

// عنصر معاملة موحّد لعرض كشف حساب الشقة (مش بيتخزن، بيتحسب وقت العرض)
public class WalletTransactionVm
{
    public string Type { get; set; } = ""; // deposit | expense | revenue | monthly_fee_due | monthly_fee_credit | adjustment
    public string Status { get; set; } = "confirmed";
    public string Id { get; set; } = "";
    public string? Number { get; set; }
    public double Amount { get; set; }
    public string Note { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}
