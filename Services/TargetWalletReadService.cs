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

    public async Task<TargetWalletSnapshot> GetApartmentAsync(
        string buildingId,
        string apartmentId,
        string? fromMonth = null,
        string? toMonth = null,
        string currencyCode = "EGP",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building is required.", nameof(buildingId));
        if (string.IsNullOrWhiteSpace(apartmentId))
            throw new ArgumentException("Apartment is required.", nameof(apartmentId));
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 ||
            currencyCode.Any(c => !char.IsAsciiLetter(c)))
            throw new ArgumentException("Currency code must contain exactly three ASCII letters.", nameof(currencyCode));
        currencyCode = currencyCode.ToUpperInvariant();
        ValidateMonth(fromMonth, nameof(fromMonth));
        ValidateMonth(toMonth, nameof(toMonth));
        if (fromMonth is not null && toMonth is not null &&
            string.CompareOrdinal(fromMonth, toMonth) > 0)
            throw new ArgumentException("The starting month cannot be after the ending month.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
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
                entry.EffectiveAtUtc
            }).ToListAsync(cancellationToken);

        var pending = await db.PaymentDeposits.AsNoTracking()
            .Where(d => d.BuildingId == buildingId && d.ApartmentId == apartmentId
                && periodIds.Contains(d.PeriodId) && d.Status == PaymentDepositStatus.Pending
                && d.CurrencyCode == currencyCode)
            .OrderByDescending(d => d.SubmittedAtUtc)
            .Select(d => new Deposit
            {
                Id = d.PaymentDepositId.ToString("D"),
                AptId = d.ApartmentId,
                Amount = d.Amount,
                Note = "",
                Status = "pending",
                CreatedAt = d.SubmittedAtUtc.ToString("O"),
                CreatedBy = ""
            }).ToListAsync(cancellationToken);

        var transactions = posted.Select(entry => new WalletTransactionVm
        {
            Type = ToLegacyType(entry.EntryKind),
            Status = "confirmed",
            Id = entry.LedgerEntryId.ToString("D"),
            Amount = entry.SignedAmount,
            Note = entry.Description ?? "",
            CreatedAt = entry.EffectiveAtUtc.ToString("O")
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
