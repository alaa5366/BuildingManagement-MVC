namespace BuildingManagementMvc.Models;

// بيانات الشهر (building.months[monthKey])
public class MonthData
{
    public Dictionary<string, object> Collections { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
    public List<Deposit> Deposits { get; set; } = new();
    public List<Revenue> Revenues { get; set; } = new();
    public Dictionary<string, double> Distribution { get; set; } = new();
    public Dictionary<string, double> RevenueDistribution { get; set; } = new();
    public Dictionary<string, double> CarryOver { get; set; } = new();
}

public class Expense
{
    public string Id { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public string Note { get; set; } = "";
    public double Amount { get; set; }
    public string Date { get; set; } = "";
    public string? Receipt { get; set; }
}

public class Revenue
{
    public string Id { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public string Note { get; set; } = "";
    public double Amount { get; set; }
    public string Date { get; set; } = "";
}

public class Deposit
{
    public string Id { get; set; } = "";
    public string Number { get; set; } = "";
    public string AptId { get; set; } = "";
    public double Amount { get; set; }
    public string Note { get; set; } = "";
    public string Status { get; set; } = "pending";
    public ReceiptData? Receipt { get; set; }
    public string CreatedAt { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public string? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public string? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancelledReason { get; set; }
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public string? UpdateReason { get; set; }
    public List<DepositEditEntry>? EditHistory { get; set; }
}

public class DepositEditEntry
{
    public string Ts { get; set; } = "";
    public string By { get; set; } = "";
    public string Reason { get; set; } = "";
    public double OldAmount { get; set; }
    public double NewAmount { get; set; }
    public string OldNote { get; set; } = "";
    public string NewNote { get; set; } = "";
}

public class WalletAdjustment
{
    public string Id { get; set; } = "";
    public string AptId { get; set; } = "";
    public double Amount { get; set; }
    public string Reason { get; set; } = "";
    public string MonthKey { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string CreatedBy { get; set; } = "";
}

public class AuditLogEntry
{
    public string Id { get; set; } = "";
    public string Ts { get; set; } = "";
    public string Action { get; set; } = "";
    public string Label { get; set; } = "";
    public string Actor { get; set; } = "";
    public string ActorRole { get; set; } = "";
    public string Details { get; set; } = "";
    public string Month { get; set; } = "";
    public string? BuildingNumber { get; set; }
    public int? AptNumber { get; set; }
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
    public string? ReceiptUrl { get; set; }
    public bool HasReceipt => !string.IsNullOrWhiteSpace(ReceiptUrl);
}

public class ReceiptData
{
    public string Url { get; set; } = "";
    public string Path { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string FileType { get; set; } = "";
    public bool Success { get; set; }
}