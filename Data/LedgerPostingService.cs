using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Atomic posting and balance queries against the canonical immutable ledger.
/// All writes are scoped to one building and one explicit open financial period.
/// </summary>
public sealed class LedgerPostingService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TimeProvider _timeProvider;

    public LedgerPostingService(
        IDbContextFactory<AppDbContext> factory,
        TimeProvider timeProvider)
    {
        _factory = factory;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> PostAsync(
        string buildingId,
        Guid periodId,
        LedgerTransactionKind kind,
        string idempotencyKey,
        Guid? actorAccountId,
        string? actorReference,
        string? reason,
        IReadOnlyCollection<LedgerPostingLine> lines,
        string? sourceSystem = null,
        string? sourceType = null,
        string? sourceKey = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building is required.", nameof(buildingId));
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128)
            throw new ArgumentException("A valid idempotency key is required.", nameof(idempotencyKey));
        if (lines is null || lines.Count == 0)
            throw new ArgumentException("At least one ledger line is required.", nameof(lines));
        if (lines.Any(x => string.IsNullOrWhiteSpace(x.ApartmentId) ||
                           x.SignedAmount == 0 ||
                           x.CurrencyCode?.Length != 3))
            throw new ArgumentException("Ledger lines require an apartment, non-zero amount and ISO currency code.", nameof(lines));
        if (lines.Select(x => x.CurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1)
            throw new ArgumentException("A transaction cannot mix currencies.", nameof(lines));
        var hasSource = sourceSystem is not null || sourceType is not null || sourceKey is not null;
        if (hasSource && (string.IsNullOrWhiteSpace(sourceSystem) ||
                          string.IsNullOrWhiteSpace(sourceType) ||
                          string.IsNullOrWhiteSpace(sourceKey)))
            throw new ArgumentException("Source system, type and key must be supplied together.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            x => x.PeriodId == periodId && x.BuildingId == buildingId, cancellationToken);
        if (period is null)
            throw new InvalidOperationException("The financial period does not belong to this building.");
        if (period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("A closed financial period cannot accept postings.");

        var existing = await db.LedgerTransactions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.BuildingId == buildingId && existing.PeriodId == periodId &&
                existing.Kind == kind && existing.Status == LedgerTransactionStatus.Posted)
            {
                await tx.CommitAsync(cancellationToken);
                return existing.LedgerTransactionId;
            }
            throw new InvalidOperationException("The idempotency key is already used by a different posting.");
        }

        var apartmentIds = lines.Select(x => x.ApartmentId).Distinct().ToArray();
        var found = await db.Apartments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && apartmentIds.Contains(x.Id))
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (found.Count != apartmentIds.Length)
            throw new InvalidOperationException("Every ledger line must reference an apartment in the target building.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var transactionId = Guid.NewGuid();
        var ledgerTransaction = new LedgerTransactionEntity
        {
            LedgerTransactionId = transactionId,
            PeriodId = periodId,
            BuildingId = buildingId,
            Kind = kind,
            Status = LedgerTransactionStatus.Posted,
            IdempotencyKey = idempotencyKey,
            SourceSystem = sourceSystem,
            SourceType = sourceType,
            SourceKey = sourceKey,
            ActorAccountId = actorAccountId,
            ActorReference = string.IsNullOrWhiteSpace(actorReference) ? null : actorReference.Trim(),
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAtUtc = now,
            PostedAtUtc = now
        };

        db.LedgerTransactions.Add(ledgerTransaction);
        foreach (var line in lines)
        {
            db.LedgerEntries.Add(new LedgerEntryEntity
            {
                LedgerEntryId = Guid.NewGuid(),
                LedgerTransactionId = transactionId,
                PeriodId = periodId,
                BuildingId = buildingId,
                ApartmentId = line.ApartmentId,
                EntryKind = line.EntryKind,
                SignedAmount = line.SignedAmount,
                CurrencyCode = line.CurrencyCode.ToUpperInvariant(),
                Description = string.IsNullOrWhiteSpace(line.Description) ? null : line.Description.Trim(),
                EffectiveAtUtc = line.EffectiveAtUtc ?? now,
                CreatedAtUtc = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return transactionId;
    }

    public async Task<decimal> GetApartmentBalanceAsync(
        string buildingId,
        string apartmentId,
        string currencyCode = "EGP",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(apartmentId))
            return 0m;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.LedgerEntries.AsNoTracking()
            .Where(e => e.BuildingId == buildingId &&
                        e.ApartmentId == apartmentId &&
                        e.CurrencyCode == currencyCode &&
                        db.LedgerTransactions.Any(t =>
                            t.LedgerTransactionId == e.LedgerTransactionId &&
                            t.BuildingId == buildingId &&
                            t.Status == LedgerTransactionStatus.Posted))
            .SumAsync(e => (decimal?)e.SignedAmount, cancellationToken) ?? 0m;
    }
}

/// <summary>A signed movement in one apartment wallet.</summary>
public sealed record LedgerPostingLine(
    string ApartmentId,
    LedgerEntryKind EntryKind,
    decimal SignedAmount,
    string CurrencyCode = "EGP",
    string? Description = null,
    DateTime? EffectiveAtUtc = null);
