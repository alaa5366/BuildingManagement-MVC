using System.Data;
using System.Text.Json;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Posts explicit, audited apartment wallet adjustments to the canonical ledger.
/// </summary>
public sealed class TargetWalletAdjustmentWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TimeProvider _timeProvider;

    public TargetWalletAdjustmentWorkflowService(
        IDbContextFactory<AppDbContext> factory,
        TimeProvider timeProvider)
    {
        _factory = factory;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> AddAsync(
        string buildingId,
        string apartmentId,
        string periodKey,
        Guid actorAccountId,
        decimal amount,
        string reason,
        string commandId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId)) throw new ArgumentException("Building is required.", nameof(buildingId));
        if (string.IsNullOrWhiteSpace(apartmentId)) throw new ArgumentException("Apartment is required.", nameof(apartmentId));
        if (string.IsNullOrWhiteSpace(periodKey) || periodKey.Length != 7 ||
            !DateOnly.TryParseExact(periodKey + "-01", "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _))
            throw new ArgumentException("Financial period must use yyyy-MM format.", nameof(periodKey));
        if (actorAccountId == Guid.Empty) throw new ArgumentException("An authenticated actor is required.", nameof(actorAccountId));
        if (amount == 0 || amount > 999999999999999m || amount < -999999999999999m ||
            decimal.Round(amount, 4, MidpointRounding.AwayFromZero) != amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Adjustment amount is invalid.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("An adjustment reason of 1 to 500 characters is required.", nameof(reason));
        if (!Guid.TryParse(commandId, out var commandGuid) || commandGuid == Guid.Empty)
            throw new ArgumentException("A valid adjustment command id is required.", nameof(commandId));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var existing = await db.LedgerTransactions.AsNoTracking().SingleOrDefaultAsync(
            item => item.BuildingId == buildingId &&
                item.IdempotencyKey == $"wallet-adjustment:{commandGuid:D}", cancellationToken);
        if (existing is not null)
        {
            var existingEntry = await db.LedgerEntries.AsNoTracking().SingleOrDefaultAsync(
                item => item.LedgerTransactionId == existing.LedgerTransactionId, cancellationToken);
            if (existing.Kind != LedgerTransactionKind.WalletAdjustment ||
                existing.ActorAccountId != actorAccountId ||
                existingEntry is null || existingEntry.BuildingId != buildingId ||
                existingEntry.ApartmentId != apartmentId ||
                existingEntry.SignedAmount != amount || existing.Reason != reason.Trim())
                throw new InvalidOperationException("The adjustment command id is already used by a different request.");
            await tx.CommitAsync(cancellationToken);
            return existing.LedgerTransactionId;
        }

        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            item => item.BuildingId == buildingId && item.PeriodKey == periodKey, cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("The selected financial period is closed or unavailable.");

        var apartmentExists = await db.Apartments.AsNoTracking().AnyAsync(
            item => item.BuildingId == buildingId && item.Id == apartmentId && !item.IsClosed, cancellationToken);
        if (!apartmentExists) throw new InvalidOperationException("The apartment is unavailable.");

        var actorIsActive = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == actorAccountId && account.AccountStatus == 1 && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);
        if (!actorIsActive) throw new InvalidOperationException("An active target account is required.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (now < period.StartsAtUtc || now >= period.EndsAtUtc)
            throw new InvalidOperationException("The adjustment must be posted inside the selected financial period.");

        var hasRole = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
            where assignment.AccountId == actorAccountId && assignment.RoleKey == "building-admin" &&
                role.IsActive && assignment.ValidFromUtc <= now &&
                (assignment.ValidToUtc == null || assignment.ValidToUtc > now) &&
                assignment.RevokedAtUtc == null && assignment.ScopeType == 1 &&
                assignment.ScopeBuildingId == buildingId && assignment.ScopeApartmentId == null
            select assignment.RoleAssignmentId).AnyAsync(cancellationToken);
        if (!hasRole) throw new InvalidOperationException("The actor has no active building-admin assignment.");

        var currencyCode = await db.BuildingSettings.AsNoTracking()
            .Where(item => item.BuildingId == buildingId)
            .Select(item => item.Currency)
            .SingleOrDefaultAsync(cancellationToken) ?? "EGP";
        if (currencyCode.Length != 3 || currencyCode.Any(c => !char.IsAsciiLetter(c)))
            throw new InvalidOperationException("Building currency configuration is invalid.");

        var transactionId = Guid.NewGuid();
        db.LedgerTransactions.Add(new LedgerTransactionEntity
        {
            LedgerTransactionId = transactionId,
            PeriodId = period.PeriodId,
            BuildingId = buildingId,
            Kind = LedgerTransactionKind.WalletAdjustment,
            Status = LedgerTransactionStatus.Posted,
            IdempotencyKey = $"wallet-adjustment:{commandGuid:D}",
            SourceSystem = "application",
            SourceType = "WalletAdjustment",
            SourceKey = commandGuid.ToString("D"),
            ActorAccountId = actorAccountId,
            ActorReference = actorAccountId.ToString("D"),
            Reason = reason.Trim(),
            CreatedAtUtc = now,
            PostedAtUtc = now
        });
        db.LedgerEntries.Add(new LedgerEntryEntity
        {
            LedgerEntryId = Guid.NewGuid(),
            LedgerTransactionId = transactionId,
            PeriodId = period.PeriodId,
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            EntryKind = LedgerEntryKind.WalletAdjustment,
            SignedAmount = amount,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            Description = reason.Trim(),
            EffectiveAtUtc = now,
            CreatedAtUtc = now
        });
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            OccurredAtUtc = now,
            ActorAccountId = actorAccountId,
            ActorReference = actorAccountId.ToString("D"),
            BuildingId = buildingId,
            EventCategory = "Finance",
            EventType = "WalletAdjustmentPosted",
            ResourceType = "LedgerTransaction",
            ResourceReference = transactionId.ToString("D"),
            Outcome = 0,
            SafeDetailsJson = JsonSerializer.Serialize(new { apartmentId, amount, currencyCode, reason = reason.Trim() })
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return transactionId;
    }
}
