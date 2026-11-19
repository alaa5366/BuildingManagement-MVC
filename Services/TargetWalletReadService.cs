using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Read-only wallet projection from the canonical financial periods, payment deposits and posted ledger.
/// This service never reads or mutates legacy monthly finance aggregates.
/// </summary>
public sealed class TargetWalletReadService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetWalletReadService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<TargetWalletSubmissionContext?> GetSubmissionContextAsync(
        string buildingId,
        string periodKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building is required.", nameof(buildingId));
        ValidateMonth(periodKey, nameof(periodKey));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var periodId = await db.FinancialPeriods.AsNoTracking()
            .Where(p => p.BuildingId == buildingId && p.PeriodKey == periodKey &&
                p.Status != FinancialPeriodStatus.Closed)
            .Select(p => (Guid?)p.PeriodId)
            .SingleOrDefaultAsync(cancellationToken);
        if (periodId is null) return null;

        var currencyCode = await db.BuildingSettings.AsNoTracking()
            .Where(settings => settings.BuildingId == buildingId)
            .Select(settings => settings.Currency)
            .SingleOrDefaultAsync(cancellationToken) ?? "EGP";

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 ||
            currencyCode.Any(c => !char.IsAsciiLetter(c)))
            throw new InvalidOperationException("The building currency configuration is invalid.");

        return new TargetWalletSubmissionContext(periodId.Value, currencyCode.ToUpperInvariant());
    }


    public async Task<IReadOnlyList<TargetPendingDeposit>> GetPendingForBuildingAsync(
        string buildingId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || buildingId.Length > 64)
            throw new ArgumentException("A valid building scope is required.", nameof(buildingId));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var rows = await (
            from deposit in db.PaymentDeposits.AsNoTracking()
            join period in db.FinancialPeriods.AsNoTracking()
                on deposit.PeriodId equals period.PeriodId
            join apartment in db.Apartments.AsNoTracking()
                on new { deposit.BuildingId, deposit.ApartmentId }
                equals new { apartment.BuildingId, ApartmentId = apartment.Id }
            where deposit.BuildingId == buildingId &&
                deposit.Status == PaymentDepositStatus.Pending &&
                period.BuildingId == buildingId
            orderby deposit.SubmittedAtUtc descending, deposit.PaymentDepositId descending
            select new
            {
                deposit.PaymentDepositId,
                deposit.ApartmentId,
                deposit.Amount,
                deposit.CurrencyCode,
                deposit.Note,
                deposit.ReceiptMetadataJson,
                deposit.SubmittedAtUtc,
                period.PeriodKey,
                apartment.Number,
                apartment.Label
            }).ToListAsync(cancellationToken);

        return rows.Select(row => new TargetPendingDeposit(
            row.PaymentDepositId,
            row.ApartmentId,
            row.Number,
            row.Label,
            row.Amount,
            row.CurrencyCode,
            row.Note ?? "",
            DeserializeReceipt(row.ReceiptMetadataJson),
            row.SubmittedAtUtc,
            row.PeriodKey)).ToList();
    }

    public async Task<TargetWalletDepositContext?> GetDepositAsync(
        Guid paymentDepositId,
        CancellationToken cancellationToken = default)
    {
        if (paymentDepositId == Guid.Empty)
            throw new ArgumentException("Deposit id is required.", nameof(paymentDepositId));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var row = await (
            from deposit in db.PaymentDeposits.AsNoTracking()
            join period in db.FinancialPeriods.AsNoTracking() on deposit.PeriodId equals period.PeriodId
            where deposit.PaymentDepositId == paymentDepositId
            select new
            {
                deposit.PaymentDepositId, deposit.BuildingId, deposit.ApartmentId, deposit.Amount,
                deposit.Note, deposit.ReceiptMetadataJson, deposit.Status, deposit.SubmittedAtUtc,
                deposit.ConfirmedAtUtc, deposit.CancelledAtUtc, deposit.ResolutionReason,
                period.PeriodKey
            }).SingleOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var status = row.Status switch
        {
            PaymentDepositStatus.Pending => "pending",
            PaymentDepositStatus.Confirmed => "confirmed",
            PaymentDepositStatus.Cancelled => "cancelled",
            PaymentDepositStatus.Reversed => "reversed",
            _ => "unknown"
        };
        var depositVm = new Deposit
        {
            Id = row.PaymentDepositId.ToString("D"),
            Number = row.PaymentDepositId.ToString("N")[..8].ToUpperInvariant(),
            AptId = row.ApartmentId,
            Amount = row.Amount,
            Note = row.Note ?? "",
            Receipt = DeserializeReceipt(row.ReceiptMetadataJson),
            Status = status,
            CreatedAt = row.SubmittedAtUtc.ToString("O"),
            ConfirmedAt = row.ConfirmedAtUtc?.ToString("O"),
            CancelledAt = row.CancelledAtUtc?.ToString("O"),
            CancelledReason = row.ResolutionReason
        };
        return new TargetWalletDepositContext(row.BuildingId, row.ApartmentId, row.PeriodKey, depositVm);
    }

    public async Task<TargetWalletAdminSnapshot> GetAdminSnapshotAsync(
        string buildingId,
        string month,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building is required.", nameof(buildingId));
        ValidateMonth(month, nameof(month));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var currencyCode = await db.BuildingSettings.AsNoTracking()
            .Where(settings => settings.BuildingId == buildingId)
            .Select(settings => settings.Currency)
            .SingleOrDefaultAsync(cancellationToken) ?? "EGP";
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 ||
            currencyCode.Any(c => !char.IsAsciiLetter(c)))
            throw new InvalidOperationException("Building currency configuration is invalid.");
        currencyCode = currencyCode.ToUpperInvariant();

        var periodsThroughMonth = db.FinancialPeriods.AsNoTracking()
            .Where(p => p.BuildingId == buildingId && string.Compare(p.PeriodKey, month) <= 0);
        var balancePeriodIds = await periodsThroughMonth.Select(p => p.PeriodId).ToListAsync(cancellationToken);

        var balanceRows = await (
            from entry in db.LedgerEntries.AsNoTracking()
            join transaction in db.LedgerTransactions.AsNoTracking()
                on entry.LedgerTransactionId equals transaction.LedgerTransactionId
            where entry.BuildingId == buildingId && balancePeriodIds.Contains(entry.PeriodId)
                && entry.CurrencyCode == currencyCode && transaction.BuildingId == buildingId
                && transaction.Status == LedgerTransactionStatus.Posted
            group entry by entry.ApartmentId into apartmentEntries
            select new { ApartmentId = apartmentEntries.Key, Balance = apartmentEntries.Sum(x => x.SignedAmount) }
        ).ToListAsync(cancellationToken);
        var balances = balanceRows.ToDictionary(x => x.ApartmentId, x => x.Balance, StringComparer.Ordinal);

        var currentPeriodIds = await db.FinancialPeriods.AsNoTracking()
            .Where(p => p.BuildingId == buildingId && p.PeriodKey == month)
            .Select(p => p.PeriodId)
            .ToListAsync(cancellationToken);

        var currentEntries = await (
            from entry in db.LedgerEntries.AsNoTracking()
            join transaction in db.LedgerTransactions.AsNoTracking()
                on entry.LedgerTransactionId equals transaction.LedgerTransactionId
            where entry.BuildingId == buildingId && currentPeriodIds.Contains(entry.PeriodId)
                && entry.CurrencyCode == currencyCode && transaction.BuildingId == buildingId
                && transaction.Status == LedgerTransactionStatus.Posted
            select new { entry.EntryKind, entry.SignedAmount }
        ).ToListAsync(cancellationToken);

        var pendingRows = await (
            from deposit in db.PaymentDeposits.AsNoTracking()
            join period in db.FinancialPeriods.AsNoTracking() on deposit.PeriodId equals period.PeriodId
            where deposit.BuildingId == buildingId && deposit.Status == PaymentDepositStatus.Pending
                && deposit.CurrencyCode == currencyCode
            orderby deposit.SubmittedAtUtc
            select new
            {
                deposit.PaymentDepositId, deposit.ApartmentId, deposit.Amount, deposit.Note,
                deposit.ReceiptMetadataJson, deposit.SubmittedAtUtc, period.PeriodKey
            }
        ).ToListAsync(cancellationToken);

        var pending = pendingRows.Select(deposit => new TargetPendingWalletDeposit(
            new Deposit
            {
                Id = deposit.PaymentDepositId.ToString("D"),
                Number = deposit.PaymentDepositId.ToString("N")[..8].ToUpperInvariant(),
                AptId = deposit.ApartmentId,
                Amount = deposit.Amount,
                Note = deposit.Note ?? "",
                Receipt = DeserializeReceipt(deposit.ReceiptMetadataJson),
                Status = "pending",
                CreatedAt = deposit.SubmittedAtUtc.ToString("O"),
                CreatedBy = ""
            },
            deposit.ApartmentId,
            deposit.PeriodKey)).ToList();

        var periodDepositRows = await db.PaymentDeposits.AsNoTracking()
            .Where(d => d.BuildingId == buildingId && currentPeriodIds.Contains(d.PeriodId) && d.CurrencyCode == currencyCode)
            .Select(d => new { d.Status, d.Amount })
            .ToListAsync(cancellationToken);

        var totals = (
            Collected: periodDepositRows.Where(d => d.Status is PaymentDepositStatus.Confirmed or PaymentDepositStatus.Reversed)
                .Sum(d => d.Status == PaymentDepositStatus.Reversed ? -d.Amount : d.Amount),
            Expenses: Math.Abs(currentEntries.Where(e => e.EntryKind == LedgerEntryKind.ExpenseAllocation).Sum(e => e.SignedAmount)),
            Revenues: currentEntries.Where(e => e.EntryKind == LedgerEntryKind.RevenueAllocation).Sum(e => e.SignedAmount),
            OpenCount: periodDepositRows.Count(d => d.Status == PaymentDepositStatus.Pending),
            ClosedCount: periodDepositRows.Count(d => d.Status is PaymentDepositStatus.Confirmed or PaymentDepositStatus.Cancelled or PaymentDepositStatus.Reversed),
            Balance: currentEntries.Sum(e => e.SignedAmount),
            TotalWallet: balances.Values.Sum());

        return new TargetWalletAdminSnapshot(balances, pending, totals);
    }

    public async Task<TargetWalletSnapshot> GetApartmentAsync(
        string buildingId,
        string apartmentId,
        string? fromMonth = null,
        string? toMonth = null,
        string? currencyCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building is required.", nameof(buildingId));
        if (string.IsNullOrWhiteSpace(apartmentId))
            throw new ArgumentException("Apartment is required.", nameof(apartmentId));
        ValidateMonth(fromMonth, nameof(fromMonth));
        ValidateMonth(toMonth, nameof(toMonth));
        if (fromMonth is not null && toMonth is not null &&
            string.CompareOrdinal(fromMonth, toMonth) > 0)
            throw new ArgumentException("The starting month cannot be after the ending month.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        currencyCode ??= await db.BuildingSettings.AsNoTracking()
            .Where(settings => settings.BuildingId == buildingId)
            .Select(settings => settings.Currency)
            .SingleOrDefaultAsync(cancellationToken) ?? "EGP";
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 ||
            currencyCode.Any(c => !char.IsAsciiLetter(c)))
            throw new ArgumentException("Currency code must contain exactly three ASCII letters.", nameof(currencyCode));
        currencyCode = currencyCode.ToUpperInvariant();

        var periodQuery = db.FinancialPeriods.AsNoTracking()
            .Where(p => p.BuildingId == buildingId);
        if (fromMonth is not null) periodQuery = periodQuery.Where(p => string.Compare(p.PeriodKey, fromMonth) >= 0);
        if (toMonth is not null) periodQuery = periodQuery.Where(p => string.Compare(p.PeriodKey, toMonth) <= 0);

        var periodIds = await periodQuery.Select(p => p.PeriodId).ToListAsync(cancellationToken);

        // The displayed balance is cumulative through the selected end month.
        // A statement range may start later, but earlier posted movements still
        // contribute to the apartment's carried-forward balance.
        var balancePeriodQuery = db.FinancialPeriods.AsNoTracking()
            .Where(p => p.BuildingId == buildingId);
        if (toMonth is not null)
            balancePeriodQuery = balancePeriodQuery.Where(p => string.Compare(p.PeriodKey, toMonth) <= 0);
        var balancePeriodIds = await balancePeriodQuery
            .Select(p => p.PeriodId)
            .ToListAsync(cancellationToken);

        var cumulativeBalance = await (
            from entry in db.LedgerEntries.AsNoTracking()
            join transaction in db.LedgerTransactions.AsNoTracking()
                on entry.LedgerTransactionId equals transaction.LedgerTransactionId
            where entry.BuildingId == buildingId && entry.ApartmentId == apartmentId
                && balancePeriodIds.Contains(entry.PeriodId)
                && transaction.BuildingId == buildingId
                && transaction.Status == LedgerTransactionStatus.Posted
                && entry.CurrencyCode == currencyCode
            select (decimal?)entry.SignedAmount)
            .SumAsync(cancellationToken) ?? 0m;

        var posted = await (
            from entry in db.LedgerEntries.AsNoTracking()
            join transaction in db.LedgerTransactions.AsNoTracking()
                on entry.LedgerTransactionId equals transaction.LedgerTransactionId
            join depositRow in db.PaymentDeposits.AsNoTracking()
                on (Guid?)transaction.LedgerTransactionId equals depositRow.PostedTransactionId into depositRows
            from deposit in depositRows.DefaultIfEmpty()
            where entry.BuildingId == buildingId && entry.ApartmentId == apartmentId
                && periodIds.Contains(entry.PeriodId)
                && transaction.BuildingId == buildingId
                && transaction.Status == LedgerTransactionStatus.Posted
                && entry.CurrencyCode == currencyCode
            orderby entry.EffectiveAtUtc descending, entry.LedgerEntryId descending
            select new
            {
                entry.LedgerEntryId,
                entry.EntryKind,
                entry.SignedAmount,
                entry.CurrencyCode,
                entry.Description,
                entry.EffectiveAtUtc,
                DepositPaymentId = deposit == null ? (Guid?)null : deposit.PaymentDepositId,
                DepositNote = deposit == null ? null : deposit.Note,
                ReceiptMetadataJson = deposit == null ? null : deposit.ReceiptMetadataJson
            }).ToListAsync(cancellationToken);

        var pendingRows = await db.PaymentDeposits.AsNoTracking()
            .Where(d => d.BuildingId == buildingId && d.ApartmentId == apartmentId
                && periodIds.Contains(d.PeriodId) && d.Status == PaymentDepositStatus.Pending
                && d.CurrencyCode == currencyCode)
            .OrderByDescending(d => d.SubmittedAtUtc)
            .Select(d => new
            {
                d.PaymentDepositId,
                d.ApartmentId,
                d.Amount,
                d.Note,
                d.ReceiptMetadataJson,
                d.SubmittedAtUtc
            })
            .ToListAsync(cancellationToken);

        var pending = pendingRows.Select(d => new Deposit
        {
            Id = d.PaymentDepositId.ToString("D"),
            Number = d.PaymentDepositId.ToString("N")[..8].ToUpperInvariant(),
            AptId = d.ApartmentId,
            Amount = d.Amount,
            Note = d.Note ?? "",
            Receipt = DeserializeReceipt(d.ReceiptMetadataJson),
            Status = "pending",
            CreatedAt = d.SubmittedAtUtc.ToString("O"),
            CreatedBy = ""
        }).ToList();

        var transactions = posted.Select(entry =>
        {
            var receipt = DeserializeReceipt(entry.ReceiptMetadataJson);
            return new WalletTransactionVm
            {
                Type = ToLegacyType(entry.EntryKind),
                Status = "confirmed",
                Id = entry.LedgerEntryId.ToString("D"),
                Number = entry.DepositPaymentId is Guid paymentDepositId
                    ? paymentDepositId.ToString("N")[..8].ToUpperInvariant()
                    : "",
                Amount = entry.SignedAmount,
                Note = entry.DepositNote ?? entry.Description ?? "",
                ReceiptUrl = receipt?.Url ?? "",
                CreatedAt = entry.EffectiveAtUtc.ToString("O")
            };
        }).ToList();

        // Cancelled submissions do not create ledger entries, but remain part of
        // the resident's financial history and must still be visible in statements.
        var cancelled = await db.PaymentDeposits.AsNoTracking()
            .Where(d => d.BuildingId == buildingId && d.ApartmentId == apartmentId
                && periodIds.Contains(d.PeriodId) && d.Status == PaymentDepositStatus.Cancelled
                && d.CurrencyCode == currencyCode)
            .OrderByDescending(d => d.CancelledAtUtc)
            .Select(d => new
            {
                d.PaymentDepositId,
                d.Amount,
                d.ResolutionReason,
                OccurredAtUtc = d.CancelledAtUtc ?? d.SubmittedAtUtc
            })
            .ToListAsync(cancellationToken);

        transactions.AddRange(cancelled.Select(deposit => new WalletTransactionVm
        {
            Type = "deposit",
            Status = "cancelled",
            Id = deposit.PaymentDepositId.ToString("D"),
            Amount = deposit.Amount,
            Note = string.IsNullOrWhiteSpace(deposit.ResolutionReason)
                ? "Deposit cancelled"
                : deposit.ResolutionReason,
            CreatedAt = deposit.OccurredAtUtc.ToString("O")
        }));

        transactions = transactions
            .OrderByDescending(item => item.CreatedAt, StringComparer.Ordinal)
            .ThenByDescending(item => item.Id, StringComparer.Ordinal)
            .ToList();

        return new TargetWalletSnapshot(
            cumulativeBalance,
            transactions,
            pending);
    }

    private static ReceiptData? DeserializeReceipt(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<ReceiptData>(json); }
        catch (JsonException) { return null; }
    }

    private static string ToLegacyType(LedgerEntryKind kind) => kind switch
    {
        LedgerEntryKind.Deposit => "deposit",
        LedgerEntryKind.MonthlyCharge => "monthly_fee_due",
        LedgerEntryKind.ExpenseAllocation => "expense",
        LedgerEntryKind.RevenueAllocation => "revenue",
        LedgerEntryKind.WalletAdjustment => "adjustment",
        LedgerEntryKind.OpeningBalance => "carry_over",
        LedgerEntryKind.Reversal => "adjustment",
        _ => "adjustment"
    };

    private static void ValidateMonth(string? month, string parameterName)
    {
        if (month is null) return;
        if (month.Length != 7 ||
            !DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _))
            throw new ArgumentException("Month must use yyyy-MM format.", parameterName);
    }
}

public sealed record TargetWalletSnapshot(
    decimal Balance,
    IReadOnlyList<WalletTransactionVm> Transactions,
    IReadOnlyList<Deposit> PendingDeposits);

public sealed record TargetWalletSubmissionContext(Guid PeriodId, string CurrencyCode);

public sealed record TargetPendingDeposit(
    Guid PaymentDepositId,
    string ApartmentId,
    int ApartmentNumber,
    string ApartmentLabel,
    decimal Amount,
    string CurrencyCode,
    string Note,
    ReceiptData? Receipt,
    DateTime SubmittedAtUtc,
    string PeriodKey);

public sealed record TargetPendingWalletDeposit(Deposit Deposit, string ApartmentId, string MonthKey);

public sealed record TargetWalletAdminSnapshot(
    IReadOnlyDictionary<string, decimal> Balances,
    IReadOnlyList<TargetPendingWalletDeposit> PendingDeposits,
    (decimal Collected, decimal Expenses, decimal Revenues, int OpenCount, int ClosedCount, decimal Balance, decimal TotalWallet) Totals);

public sealed record TargetWalletDepositContext(string BuildingId, string ApartmentId, string MonthKey, Deposit Deposit);
