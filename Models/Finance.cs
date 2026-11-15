namespace BuildingManagementMvc.Models;

// بيانات الشهر (building.months[monthKey])
/// <summary>
/// All financial data of one month: expenses, revenues, deposits, per-apartment shares and carried-over balances.
/// </summary>
public class MonthData
{
    /// <summary>
    /// Gets or sets the collections.
    /// </summary>
    public Dictionary<string, object> Collections { get; set; } = new();
    /// <summary>
    /// Gets or sets the expenses.
    /// </summary>
    public List<Expense> Expenses { get; set; } = new();
    /// <summary>
    /// Gets or sets the deposits.
    /// </summary>
    public List<Deposit> Deposits { get; set; } = new();
    /// <summary>
    /// Gets or sets the revenues.
    /// </summary>
    public List<Revenue> Revenues { get; set; } = new();
    /// <summary>
    /// Gets or sets the distribution.
    /// </summary>
    public Dictionary<string, decimal> Distribution { get; set; } = new();
    /// <summary>
    /// Gets or sets the revenue distribution.
    /// </summary>
    public Dictionary<string, decimal> RevenueDistribution { get; set; } = new();
    /// <summary>
    /// Gets or sets the carry over.
    /// </summary>
    public Dictionary<string, decimal> CarryOver { get; set; } = new();
}

/// <summary>
/// A monthly expense of the building.
/// </summary>
public class Expense
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the category.
    /// </summary>
    public string CategoryId { get; set; } = "";
    /// <summary>
    /// Gets or sets the note.
    /// </summary>
    public string Note { get; set; } = "";
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    public string Date { get; set; } = "";
    /// <summary>
    /// Gets or sets the receipt.
    /// </summary>
    public string? Receipt { get; set; }
}

/// <summary>
/// A monthly revenue of the building.
/// </summary>
public class Revenue
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the category.
    /// </summary>
    public string CategoryId { get; set; } = "";
    /// <summary>
    /// Gets or sets the note.
    /// </summary>
    public string Note { get; set; } = "";
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    public string Date { get; set; } = "";
}

/// <summary>
/// A payment submitted for an apartment. It only counts toward the wallet balance once it is confirmed.
/// </summary>
public class Deposit
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
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string AptId { get; set; } = "";
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the note.
    /// </summary>
    public string Note { get; set; } = "";
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "pending";
    /// <summary>
    /// Gets or sets the receipt.
    /// </summary>
    public ReceiptData? Receipt { get; set; }
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string CreatedBy { get; set; } = "";
    /// <summary>
    /// Gets or sets the confirmed date and time.
    /// </summary>
    public string? ConfirmedAt { get; set; }
    /// <summary>
    /// Gets or sets the confirmed by.
    /// </summary>
    public string? ConfirmedBy { get; set; }
    /// <summary>
    /// Gets or sets the cancelled date and time.
    /// </summary>
    public string? CancelledAt { get; set; }
    /// <summary>
    /// Gets or sets the cancelled by.
    /// </summary>
    public string? CancelledBy { get; set; }
    /// <summary>
    /// Gets or sets the cancelled reason.
    /// </summary>
    public string? CancelledReason { get; set; }
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public string? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
    /// <summary>
    /// Gets or sets the update reason.
    /// </summary>
    public string? UpdateReason { get; set; }
    /// <summary>
    /// Gets or sets the edit history.
    /// </summary>
    public List<DepositEditEntry>? EditHistory { get; set; }
}

/// <summary>
/// One entry of a deposit's edit history.
/// </summary>
public class DepositEditEntry
{
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public string Ts { get; set; } = "";
    /// <summary>
    /// Gets or sets the by.
    /// </summary>
    public string By { get; set; } = "";
    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string Reason { get; set; } = "";
    /// <summary>
    /// Gets or sets the old amount.
    /// </summary>
    public decimal OldAmount { get; set; }
    /// <summary>
    /// Gets or sets the new amount.
    /// </summary>
    public decimal NewAmount { get; set; }
    /// <summary>
    /// Gets or sets the old note.
    /// </summary>
    public string OldNote { get; set; } = "";
    /// <summary>
    /// Gets or sets the new note.
    /// </summary>
    public string NewNote { get; set; } = "";
}

/// <summary>
/// A manual correction of an apartment's wallet balance made by an admin.
/// </summary>
public class WalletAdjustment
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string AptId { get; set; } = "";
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string Reason { get; set; } = "";
    /// <summary>
    /// Gets or sets the month key.
    /// </summary>
    public string MonthKey { get; set; } = "";
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string CreatedBy { get; set; } = "";
}

/// <summary>
/// One audit log entry (who did what, when and on which apartment).
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public string Ts { get; set; } = "";
    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    public string Action { get; set; } = "";
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the actor.
    /// </summary>
    public string Actor { get; set; } = "";
    /// <summary>
    /// Gets or sets the actor role.
    /// </summary>
    public string ActorRole { get; set; } = "";
    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    public string Details { get; set; } = "";
    /// <summary>
    /// Gets or sets the month.
    /// </summary>
    public string Month { get; set; } = "";
    /// <summary>
    /// Gets or sets the building number.
    /// </summary>
    public string? BuildingNumber { get; set; }
    /// <summary>
    /// Gets or sets the apartment number.
    /// </summary>
    public int? AptNumber { get; set; }
    public string Severity { get; set; } = "info";
}

/// <summary>
/// One row of an apartment's account statement (deposit, expense share, revenue share, adjustment or fee).
/// </summary>
public class WalletTransactionVm
{
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string Type { get; set; } = "";
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "confirmed";
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the number.
    /// </summary>
    public string? Number { get; set; }
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the note.
    /// </summary>
    public string Note { get; set; } = "";
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the receipt URL.
    /// </summary>
    public string? ReceiptUrl { get; set; }
    /// <summary>
    /// Gets a value indicating whether it has receipt.
    /// </summary>
    public bool HasReceipt => !string.IsNullOrWhiteSpace(ReceiptUrl);
}

/// <summary>
/// Data extracted from a receipt image by OCR (amount, reference, date, account, sender).
/// </summary>
public class ReceiptData
{
    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    public string Url { get; set; } = "";
    /// <summary>
    /// Gets or sets the path.
    /// </summary>
    public string Path { get; set; } = "";
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = "";
    /// <summary>
    /// Gets or sets the file size.
    /// </summary>
    public long FileSize { get; set; }
    /// <summary>
    /// Gets or sets the file type.
    /// </summary>
    public string FileType { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is success.
    /// </summary>
    public bool Success { get; set; }
}