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

    /// <summary>Posts a balanced ledger transaction.</summary>
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
        if (string.IsNullOrWhiteSpace(buildingId) || buildingId.Length > 64)
            throw new ArgumentException("Building is required and must not exceed 64 characters.", nameof(buildingId));
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128)
            throw new ArgumentException("A valid idempotency key is required.", nameof(idempotencyKey));
        if (lines is null || lines.Count == 0)
            throw new ArgumentException("At least one ledger line is required.", nameof(lines));
        if (lines.Any(x => string.IsNullOrWhiteSpace(x.ApartmentId) ||
                           x.SignedAmount == 0 ||
                           x.CurrencyCode?.Length != 3 ||
                           x.CurrencyCode.Any(c => !((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))) ||
                           x.ApartmentId.Length > 64 ||
                           (x.Description?.Length ?? 0) > 500))
            throw new ArgumentException("Ledger lines require an apartment, non-zero amount and ISO currency code.", nameof(lines));
        if (lines.Select(x => x.CurrencyCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1)
            throw new ArgumentException("A transaction cannot mix currencies.", nameof(lines));
        if (lines.Any(x => x.EffectiveAtUtc.HasValue && x.EffectiveAtUtc.Value.Kind != DateTimeKind.Utc))
            throw new ArgumentException("Explicit ledger effective timestamps must be UTC.", nameof(lines));
        if (lines.Any(x => x.EffectiveAtUtc.HasValue) && lines.Any(x => !x.EffectiveAtUtc.HasValue))
            throw new ArgumentException("Either provide effective timestamps for every line or omit them for every line.", nameof(lines));
        var hasSource = sourceSystem is not null || sourceType is not null || sourceKey is not null;
        if (hasSource && (string.IsNullOrWhiteSpace(sourceSystem) ||
                          string.IsNullOrWhiteSpace(sourceType) ||
                          string.IsNullOrWhiteSpace(sourceKey)))
            throw new ArgumentException("Source system, type and key must be supplied together.");
        if ((sourceSystem?.Length ?? 0) > 80 || (sourceType?.Length ?? 0) > 80 ||
            (sourceKey?.Length ?? 0) > 256 || (actorReference?.Length ?? 0) > 128 ||
            (reason?.Length ?? 0) > 1000)
            throw new ArgumentException("A source or actor field exceeds its configured storage limit.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);

        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            x => x.PeriodId == periodId && x.BuildingId == buildingId, cancellationToken);
        if (period is null)
            throw new InvalidOperationException("The financial period does not belong to this building.");
        var existing = await db.LedgerTransactions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.BuildingId == buildingId && existing.PeriodId == periodId &&
                existing.Kind == kind && existing.Status == LedgerTransactionStatus.Posted &&
                existing.SourceSystem == sourceSystem &&
                existing.SourceType == sourceType &&
                existing.SourceKey == sourceKey &&
                existing.ActorAccountId == actorAccountId &&
                existing.ActorReference == (string.IsNullOrWhiteSpace(actorReference) ? null : actorReference.Trim()) &&
                existing.Reason == (string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()))
            {
                var persistedLines = await db.LedgerEntries.AsNoTracking()
                    .Where(x => x.LedgerTransactionId == existing.LedgerTransactionId)
                    .Select(x => new
                    {
                        x.ApartmentId,
                        x.EntryKind,
                        x.SignedAmount,
                        x.CurrencyCode,
                        x.Description,
                        x.EffectiveAtUtc
                    })
                    .ToListAsync(cancellationToken);

                var compareEffectiveAt = lines.All(x => x.EffectiveAtUtc.HasValue);
                var requestedPayload = lines
                    .Select(x => (
                        ApartmentId: x.ApartmentId,
                        EntryKind: x.EntryKind,
                        SignedAmount: x.SignedAmount,
                        CurrencyCode: x.CurrencyCode.ToUpperInvariant(),
                        Description: string.IsNullOrWhiteSpace(x.Description) ? null : x.Description.Trim(),
                        EffectiveAtUtc: compareEffectiveAt ? x.EffectiveAtUtc : null))
                    .OrderBy(x => x.ApartmentId, StringComparer.Ordinal)
                    .ThenBy(x => x.EntryKind)
                    .ThenBy(x => x.SignedAmount)
                    .ThenBy(x => x.CurrencyCode, StringComparer.Ordinal)
                    .ThenBy(x => x.Description, StringComparer.Ordinal)
                    .ToArray();

                var persistedPayload = persistedLines
                    .Select(x => (
                        ApartmentId: x.ApartmentId,
                        EntryKind: x.EntryKind,
                        SignedAmount: x.SignedAmount,
                        CurrencyCode: x.CurrencyCode,
                        Description: x.Description,
                        EffectiveAtUtc: compareEffectiveAt ? (DateTime?)x.EffectiveAtUtc : null))
                    .OrderBy(x => x.ApartmentId, StringComparer.Ordinal)
                    .ThenBy(x => x.EntryKind)
                    .ThenBy(x => x.SignedAmount)
                    .ThenBy(x => x.CurrencyCode, StringComparer.Ordinal)
                    .ThenBy(x => x.Description, StringComparer.Ordinal)
                    .ToArray();

                if (!requestedPayload.SequenceEqual(persistedPayload))
                    throw new InvalidOperationException("The idempotency key was already used with a different ledger payload.");

                await tx.CommitAsync(cancellationToken);
                return existing.LedgerTransactionId;
            }
            throw new InvalidOperationException("The idempotency key is already used by a different posting.");
        }

        if (period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("A closed financial period cannot accept postings.");

        var apartmentIds = lines.Select(x => x.ApartmentId).Distinct().ToArray();
        var found = await db.Apartments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && apartmentIds.Contains(x.Id))
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (found.Count != apartmentIds.Length)
            throw new InvalidOperationException("Every ledger line must reference an apartment in the target building.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var line in lines)
        {
            var effectiveAt = line.EffectiveAtUtc ?? now;
            if (effectiveAt < period.StartsAtUtc || effectiveAt >= period.EndsAtUtc)
                throw new InvalidOperationException("Every ledger entry must fall within its financial period.");
        }

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

    /// <summary>Gets the posted ledger balance for an apartment.</summary>
    public async Task<decimal> GetApartmentBalanceAsync(
        string buildingId,
        string apartmentId,
        string currencyCode = "EGP",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(apartmentId))
            return 0m;
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 ||
            currencyCode.Any(c => !((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))))
            throw new ArgumentException("Currency code must contain exactly three ASCII letters.", nameof(currencyCode));

        var normalizedCurrency = currencyCode.ToUpperInvariant();
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.LedgerEntries.AsNoTracking()
            .Where(e => e.BuildingId == buildingId &&
                        e.ApartmentId == apartmentId &&
                        e.CurrencyCode == normalizedCurrency &&
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
