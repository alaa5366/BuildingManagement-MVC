namespace BuildingManagementMvc.Data.Entities;

/// <summary>Lifecycle states for a building-local financial period.</summary>
public enum FinancialPeriodStatus : byte
{
    Open = 0,
    Closed = 1,
    Reopened = 2
}

/// <summary>Supported deposit transitions retained with their original evidence.</summary>
public enum PaymentDepositStatus : byte
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Reversed = 3
}

/// <summary>Posting lifecycle for an atomic financial transaction.</summary>
public enum LedgerTransactionStatus : byte
{
    Draft = 0,
    Posted = 1,
    Cancelled = 2
}

/// <summary>Financial business operation that produced a ledger transaction.</summary>
public enum LedgerTransactionKind : byte
{
    Deposit = 1,
    Expense = 2,
    Revenue = 3,
    WalletAdjustment = 4,
    MonthlyCharge = 5,
    OpeningBalance = 6,
    Reversal = 7,
    Allocation = 8
}

/// <summary>Wallet component represented by one immutable ledger entry.</summary>
public enum LedgerEntryKind : byte
{
    Deposit = 1,
    MonthlyCharge = 2,
    ExpenseAllocation = 3,
    RevenueAllocation = 4,
    WalletAdjustment = 5,
    OpeningBalance = 6,
    Reversal = 7
}

/// <summary>Lifecycle states for a deterministic allocation batch.</summary>
public enum FinancialAllocationStatus : byte
{
    Draft = 0,
    Completed = 1,
    Cancelled = 2
}

/// <summary>Execution states for a comparison of target and legacy financial results.</summary>
public enum FinancialReconciliationStatus : byte
{
    Running = 0,
    Completed = 1,
    Failed = 2
}

/// <summary>Building-local accounting period with an explicit IANA time zone.</summary>
public sealed class FinancialPeriodEntity
{
    public Guid PeriodId { get; set; }
    public string BuildingId { get; set; } = "";
    public string PeriodKey { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
    public FinancialPeriodStatus Status { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public Guid? ClosedByAccountId { get; set; }
    public string? CloseReason { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>New deposit workflow record. Legacy deposits remain available as source evidence.</summary>
public sealed class PaymentDepositEntity
{
    public Guid PaymentDepositId { get; set; }
    public Guid PeriodId { get; set; }
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public Guid? PostedTransactionId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "EGP";
    public PaymentDepositStatus Status { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public Guid? ConfirmedByAccountId { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? CancelledByAccountId { get; set; }
    public DateTime? ReversedAtUtc { get; set; }
    public Guid? ReversedByAccountId { get; set; }
    public Guid? ReversalTransactionId { get; set; }
    public string? ResolutionReason { get; set; }
    public string? Note { get; set; }
    public string? ReceiptMetadataJson { get; set; }
    public string SourceSystem { get; set; } = "application";
    public string? SourceKey { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Atomic, idempotent financial posting group.</summary>
public sealed class LedgerTransactionEntity
{
    public Guid LedgerTransactionId { get; set; }
    public Guid PeriodId { get; set; }
    public string BuildingId { get; set; } = "";
    public LedgerTransactionKind Kind { get; set; }
    public LedgerTransactionStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = "";
    public string? SourceSystem { get; set; }
    public string? SourceType { get; set; }
    public string? SourceKey { get; set; }
    public Guid? ReversesTransactionId { get; set; }
    public Guid? ActorAccountId { get; set; }
    public string? ActorReference { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>Immutable signed apartment wallet movement after its parent transaction is posted.</summary>
public sealed class LedgerEntryEntity
{
    public Guid LedgerEntryId { get; set; }
    public Guid LedgerTransactionId { get; set; }
    public Guid PeriodId { get; set; }
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public LedgerEntryKind EntryKind { get; set; }
    public decimal SignedAmount { get; set; }
    public string CurrencyCode { get; set; } = "EGP";
    public string? Description { get; set; }
    public DateTime EffectiveAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>Unique source-to-posting provenance link for migrated or newly posted finance records.</summary>
public sealed class FinancialSourceLinkEntity
{
    public Guid FinancialSourceLinkId { get; set; }
    public string SourceSystem { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string SourceKey { get; set; } = "";
    public Guid LedgerTransactionId { get; set; }
    public byte[]? SourceFingerprint { get; set; }
    public DateTime LinkedAtUtc { get; set; }
}

/// <summary>Frozen, versioned allocation input and result summary.</summary>
public sealed class FinancialAllocationBatchEntity
{
    public Guid AllocationBatchId { get; set; }
    public Guid PeriodId { get; set; }
    public string BuildingId { get; set; } = "";
    public Guid SourceTransactionId { get; set; }
    public string BasisKey { get; set; } = "";
    public string AlgorithmVersion { get; set; } = "";
    public byte[] InputFingerprint { get; set; } = Array.Empty<byte>();
    public decimal SourceAmount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public string CurrencyCode { get; set; } = "EGP";
    public FinancialAllocationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? CreatedByAccountId { get; set; }
}

/// <summary>One deterministic apartment allocation result.</summary>
public sealed class FinancialAllocationLineEntity
{
    public Guid AllocationLineId { get; set; }
    public Guid AllocationBatchId { get; set; }
    public Guid PeriodId { get; set; }
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public decimal Weight { get; set; }
    public decimal ExactShare { get; set; }
    public decimal AllocatedAmount { get; set; }
    public int RemainderRank { get; set; }
}

/// <summary>Immutable run metadata for comparing target and legacy finance calculations.</summary>
public sealed class FinancialReconciliationRunEntity
{
    public Guid ReconciliationRunId { get; set; }
    public string RuleVersion { get; set; } = "";
    public string SourceSystem { get; set; } = "legacy-sql";
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public FinancialReconciliationStatus Status { get; set; }
    public Guid? StartedByAccountId { get; set; }
    public string? SafeSummary { get; set; }
}

/// <summary>Classified, non-corrective variance captured by a reconciliation run.</summary>
public sealed class FinancialReconciliationDifferenceEntity
{
    public Guid ReconciliationDifferenceId { get; set; }
    public Guid ReconciliationRunId { get; set; }
    public string DifferenceType { get; set; } = "";
    public string? BuildingId { get; set; }
    public string? ApartmentId { get; set; }
    public string? PeriodKey { get; set; }
    public string? SourceType { get; set; }
    public string? SourceKey { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? TargetAmount { get; set; }
    public decimal? DifferenceAmount { get; set; }
    public string Classification { get; set; } = "unresolved";
    public string? Disposition { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? ReviewedByAccountId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}


/// <summary>
/// Immutable monthly invoice issue. One row per apartment and financial month;
/// the identity value is the stable sequential invoice serial.
/// </summary>
public sealed class InvoiceIssueEntity
{
    public long InvoiceSerial { get; set; }
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public string MonthKey { get; set; } = "";
    public string? InvoiceNo { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public string? SnapshotJson { get; set; }
}
