using System.Data;
using System.Text.Json;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Canonical deposit lifecycle. Confirmation and ledger posting are one serializable transaction;
/// cancellation is permitted only while the deposit is pending.
/// </summary>
public sealed class TargetDepositWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TimeProvider _timeProvider;

    public TargetDepositWorkflowService(
        IDbContextFactory<AppDbContext> factory,
        TimeProvider timeProvider)
    {
        _factory = factory;
        _timeProvider = timeProvider;
    }

    /// <summary>Creates a pending deposit submission for the specified resident and period.</summary>
    public async Task<Guid> CreatePendingAsync(
        string buildingId,
        string apartmentId,
        Guid periodId,
        Guid actorAccountId,
        decimal amount,
        string currencyCode = "EGP",
        string? sourceKey = null,
        CancellationToken cancellationToken = default,
        string? note = null,
        string? receiptMetadataJson = null)
    {
        if (note?.Length > 1000) throw new ArgumentException("Deposit note cannot exceed 1000 characters.", nameof(note));
        if (receiptMetadataJson is { Length: > 8000 }) throw new ArgumentException("Receipt metadata is too large.", nameof(receiptMetadataJson));
        if (!string.IsNullOrWhiteSpace(receiptMetadataJson))
        {
            try { using var receiptDocument = JsonDocument.Parse(receiptMetadataJson); }
            catch (JsonException ex) { throw new ArgumentException("Receipt metadata must be valid JSON.", nameof(receiptMetadataJson), ex); }
        }
        if (actorAccountId == Guid.Empty) throw new ArgumentException("An authenticated actor is required.", nameof(actorAccountId));
        if (string.IsNullOrWhiteSpace(buildingId) || buildingId.Length > 64)
            throw new ArgumentException("A valid building is required.", nameof(buildingId));
        if (string.IsNullOrWhiteSpace(apartmentId) || apartmentId.Length > 64)
            throw new ArgumentException("A valid apartment is required.", nameof(apartmentId));
        if (amount <= 0 || decimal.Round(amount, 4, MidpointRounding.AwayFromZero) != amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive and precise to at most four decimal places.");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3 ||
            currencyCode.Any(c => !char.IsAsciiLetter(c)))
            throw new ArgumentException("Currency must contain exactly three ASCII letters.", nameof(currencyCode));
        if (sourceKey is { Length: > 256 })
            throw new ArgumentException("Source key is too long.", nameof(sourceKey));
        if (sourceKey is not null && string.IsNullOrWhiteSpace(sourceKey))
            throw new ArgumentException("Source key cannot be blank.", nameof(sourceKey));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            p => p.PeriodId == periodId && p.BuildingId == buildingId, cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("An open financial period for this building is required.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (now < period.StartsAtUtc || now >= period.EndsAtUtc)
            throw new InvalidOperationException("A deposit can only be submitted inside its financial period.");
        var actorIsActive = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == actorAccountId && account.AccountStatus == 1 && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);
        if (!actorIsActive)
            throw new InvalidOperationException("An active target account is required to submit a deposit.");
        if (!await HasScopedRoleAsync(db, actorAccountId, "resident", buildingId, apartmentId, now, cancellationToken))
            throw new InvalidOperationException("The actor has no active resident assignment for this apartment.");

        var apartmentExists = await db.Apartments.AsNoTracking()
            .AnyAsync(a => a.BuildingId == buildingId && a.Id == apartmentId, cancellationToken);
        if (!apartmentExists)
            throw new InvalidOperationException("The apartment does not belong to this building.");

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var normalizedReceiptMetadata = string.IsNullOrWhiteSpace(receiptMetadataJson)
            ? null
            : receiptMetadataJson;

        if (sourceKey is not null)
        {
            var existing = await db.PaymentDeposits.AsNoTracking().SingleOrDefaultAsync(
                d => d.SourceSystem == "application" && d.SourceKey == sourceKey, cancellationToken);
            if (existing is not null)
            {
                if (existing.BuildingId == buildingId && existing.ApartmentId == apartmentId &&
                    existing.PeriodId == periodId && existing.Amount == amount &&
                    existing.CurrencyCode == currencyCode.ToUpperInvariant() &&
                    existing.Note == normalizedNote &&
                    existing.ReceiptMetadataJson == normalizedReceiptMetadata)
                {
                    await tx.CommitAsync(cancellationToken);
                    return existing.PaymentDepositId;
                }
                throw new InvalidOperationException("The deposit source key is already used by a different request.");
            }
        }

        var id = Guid.NewGuid();
        db.PaymentDeposits.Add(new PaymentDepositEntity
        {
            PaymentDepositId = id,
            PeriodId = periodId,
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            Amount = amount,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            Status = PaymentDepositStatus.Pending,
            SubmittedAtUtc = now,
            Note = normalizedNote,
            ReceiptMetadataJson = normalizedReceiptMetadata,
            SourceSystem = "application",
            SourceKey = sourceKey
        });
        db.AuditEvents.Add(CreateAuditEvent(
            buildingId, actorAccountId, "PaymentDepositSubmitted", apartmentId, now,
            new { paymentDepositId = id, amount, currencyCode = currencyCode.ToUpperInvariant() }));
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return id;
    }

    /// <summary>Confirms a pending deposit and posts the corresponding financial records.</summary>
    public async Task<Guid?> ConfirmAsync(
        Guid paymentDepositId,
        Guid actorAccountId,
        string? actorReference = null,
        CancellationToken cancellationToken = default)
    {
        if (paymentDepositId == Guid.Empty) throw new ArgumentException("Deposit id is required.", nameof(paymentDepositId));
        if (actorAccountId == Guid.Empty) throw new ArgumentException("An authenticated actor is required.", nameof(actorAccountId));
        if (actorReference?.Length > 128) throw new ArgumentException("Actor reference is too long.", nameof(actorReference));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var deposit = await db.PaymentDeposits.SingleOrDefaultAsync(
            d => d.PaymentDepositId == paymentDepositId, cancellationToken);
        if (deposit is null) return null;

        if (deposit.Status == PaymentDepositStatus.Confirmed)
        {
            if (deposit.PostedTransactionId is null)
                throw new InvalidOperationException("Confirmed deposit has no posted ledger transaction.");
            var confirmedPostingExists = await db.LedgerTransactions.AsNoTracking().AnyAsync(
                item => item.LedgerTransactionId == deposit.PostedTransactionId &&
                    item.PeriodId == deposit.PeriodId && item.BuildingId == deposit.BuildingId &&
                    item.Kind == LedgerTransactionKind.Deposit &&
                    item.Status == LedgerTransactionStatus.Posted,
                cancellationToken);
            if (!confirmedPostingExists)
                throw new InvalidOperationException("Confirmed deposit ledger provenance is inconsistent.");
            await tx.CommitAsync(cancellationToken);
            return deposit.PostedTransactionId;
        }
        if (deposit.Status != PaymentDepositStatus.Pending)
            throw new InvalidOperationException("Only a pending deposit can be confirmed.");

        var actorIsActive = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == actorAccountId && account.AccountStatus == 1 && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);
        if (!actorIsActive)
            throw new InvalidOperationException("An active target account is required to confirm a deposit.");
        var authorizationTime = _timeProvider.GetUtcNow().UtcDateTime;
        if (!await HasFinanceManagementRoleAsync(db, actorAccountId, deposit.BuildingId, authorizationTime, cancellationToken))
            throw new InvalidOperationException("The actor has no active finance-management assignment for this building.");

        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            p => p.PeriodId == deposit.PeriodId && p.BuildingId == deposit.BuildingId, cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("The deposit financial period is closed or unavailable.");

        // Pending submissions may be reviewed after their source month ends while
        // that financial period remains explicitly open. The ledger keeps the
        // original PeriodId and records the actual posting timestamp separately.
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var transactionId = Guid.NewGuid();
        var sourceKey = deposit.SourceKey ?? deposit.PaymentDepositId.ToString("D");
        var ledger = new LedgerTransactionEntity
        {
            LedgerTransactionId = transactionId,
            PeriodId = deposit.PeriodId,
            BuildingId = deposit.BuildingId,
            Kind = LedgerTransactionKind.Deposit,
            Status = LedgerTransactionStatus.Posted,
            IdempotencyKey = $"payment-deposit:{deposit.PaymentDepositId:D}:confirm",
            SourceSystem = deposit.SourceSystem,
            SourceType = "PaymentDeposit",
            SourceKey = sourceKey,
            ActorAccountId = actorAccountId,
            ActorReference = string.IsNullOrWhiteSpace(actorReference) ? actorAccountId.ToString("D") : actorReference.Trim(),
            Reason = "Deposit confirmed",
            CreatedAtUtc = now,
            PostedAtUtc = now
        };
        db.LedgerTransactions.Add(ledger);
        db.LedgerEntries.Add(new LedgerEntryEntity
        {
            LedgerEntryId = Guid.NewGuid(),
            LedgerTransactionId = transactionId,
            PeriodId = deposit.PeriodId,
            BuildingId = deposit.BuildingId,
            ApartmentId = deposit.ApartmentId,
            EntryKind = LedgerEntryKind.Deposit,
            SignedAmount = deposit.Amount,
            CurrencyCode = deposit.CurrencyCode,
            Description = "Payment deposit",
            EffectiveAtUtc = now,
            CreatedAtUtc = now
        });

        deposit.Status = PaymentDepositStatus.Confirmed;
        deposit.PostedTransactionId = transactionId;
        deposit.ConfirmedAtUtc = now;
        deposit.ConfirmedByAccountId = actorAccountId;
        db.AuditEvents.Add(CreateAuditEvent(
            deposit.BuildingId, actorAccountId, "PaymentDepositConfirmed", deposit.ApartmentId, now,
            new { paymentDepositId = deposit.PaymentDepositId, amount = deposit.Amount, currencyCode = deposit.CurrencyCode, transactionId }));
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return transactionId;
    }


    /// <summary>Updates the details of a pending deposit submission.</summary>
    public async Task<bool> UpdatePendingAsync(
        Guid paymentDepositId,
        Guid actorAccountId,
        decimal amount,
        string? note,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (paymentDepositId == Guid.Empty) throw new ArgumentException("Deposit id is required.", nameof(paymentDepositId));
        if (actorAccountId == Guid.Empty) throw new ArgumentException("An authenticated actor is required.", nameof(actorAccountId));
        if (amount <= 0 || amount > 999999999999999m ||
            decimal.Round(amount, 4, MidpointRounding.AwayFromZero) != amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Deposit amount must be positive and precise to at most four decimal places.");
        if (note?.Length > 1000) throw new ArgumentException("Deposit note cannot exceed 1000 characters.", nameof(note));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("An edit reason of 1 to 500 characters is required.", nameof(reason));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var deposit = await db.PaymentDeposits.SingleOrDefaultAsync(
            item => item.PaymentDepositId == paymentDepositId, cancellationToken);
        if (deposit is null) return false;
        if (deposit.Status != PaymentDepositStatus.Pending)
            throw new InvalidOperationException("Only a pending deposit can be edited.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var actorIsActive = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == actorAccountId && account.AccountStatus == 1 && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);
        if (!actorIsActive)
            throw new InvalidOperationException("An active target account is required to edit a deposit.");
        if (!await HasFinanceManagementRoleAsync(db, actorAccountId, deposit.BuildingId, now, cancellationToken))
            throw new InvalidOperationException("The actor has no active finance-management assignment for this building.");

        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            item => item.PeriodId == deposit.PeriodId && item.BuildingId == deposit.BuildingId,
            cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("A deposit can only be edited while its source financial period remains open.");

        var oldAmount = deposit.Amount;
        var oldNote = deposit.Note;
        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        deposit.Amount = amount;
        deposit.Note = normalizedNote;
        db.AuditEvents.Add(CreateAuditEvent(
            deposit.BuildingId, actorAccountId, "PaymentDepositUpdated", deposit.ApartmentId, now,
            new { paymentDepositId = deposit.PaymentDepositId, oldAmount, newAmount = amount, oldNote, newNote = normalizedNote, reason = reason.Trim() }));
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Reverses a previously confirmed deposit using a compensating transaction.</summary>
    public async Task<Guid?> ReverseConfirmedAsync(
        Guid paymentDepositId,
        Guid actorAccountId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (paymentDepositId == Guid.Empty) throw new ArgumentException("Deposit id is required.", nameof(paymentDepositId));
        if (actorAccountId == Guid.Empty) throw new ArgumentException("An authenticated actor is required.", nameof(actorAccountId));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("A reversal reason of 1 to 500 characters is required.", nameof(reason));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var deposit = await db.PaymentDeposits.SingleOrDefaultAsync(
            d => d.PaymentDepositId == paymentDepositId, cancellationToken);
        if (deposit is null) return null;

        if (deposit.Status == PaymentDepositStatus.Reversed)
        {
            if (deposit.ReversalTransactionId is null)
                throw new InvalidOperationException("Reversed deposit has no reversal transaction.");
            var existingReversal = await db.LedgerTransactions.AsNoTracking().AnyAsync(
                item => item.LedgerTransactionId == deposit.ReversalTransactionId &&
                    item.PeriodId == deposit.PeriodId && item.BuildingId == deposit.BuildingId &&
                    item.Kind == LedgerTransactionKind.Reversal &&
                    item.Status == LedgerTransactionStatus.Posted &&
                    item.ReversesTransactionId == deposit.PostedTransactionId,
                cancellationToken);
            if (!existingReversal)
                throw new InvalidOperationException("Deposit reversal provenance is inconsistent.");
            await tx.CommitAsync(cancellationToken);
            return deposit.ReversalTransactionId;
        }
        if (deposit.Status != PaymentDepositStatus.Confirmed || deposit.PostedTransactionId is null)
            throw new InvalidOperationException("Only a confirmed deposit can be reversed.");

        var actorIsActive = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == actorAccountId && account.AccountStatus == 1 && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);
        if (!actorIsActive)
            throw new InvalidOperationException("An active target account is required to reverse a deposit.");
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!await HasFinanceManagementRoleAsync(db, actorAccountId, deposit.BuildingId, now, cancellationToken))
            throw new InvalidOperationException("The actor has no active finance-management assignment for this building.");

        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            p => p.PeriodId == deposit.PeriodId && p.BuildingId == deposit.BuildingId, cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed ||
            now < period.StartsAtUtc || now >= period.EndsAtUtc)
            throw new InvalidOperationException("The deposit can only be reversed inside its open financial period.");

        var original = await db.LedgerTransactions.AsNoTracking().SingleOrDefaultAsync(
            item => item.LedgerTransactionId == deposit.PostedTransactionId &&
                item.PeriodId == deposit.PeriodId && item.BuildingId == deposit.BuildingId &&
                item.Kind == LedgerTransactionKind.Deposit && item.Status == LedgerTransactionStatus.Posted,
            cancellationToken);
        if (original is null)
            throw new InvalidOperationException("The original deposit posting is unavailable.");

        var originalEntries = await db.LedgerEntries.AsNoTracking()
            .Where(item => item.LedgerTransactionId == original.LedgerTransactionId &&
                item.BuildingId == deposit.BuildingId && item.ApartmentId == deposit.ApartmentId &&
                item.EntryKind == LedgerEntryKind.Deposit && item.CurrencyCode == deposit.CurrencyCode)
            .ToListAsync(cancellationToken);
        if (originalEntries.Count != 1 || originalEntries[0].SignedAmount != deposit.Amount)
            throw new InvalidOperationException("The original deposit ledger amount does not match its source record.");

        var reversalId = Guid.NewGuid();
        db.LedgerTransactions.Add(new LedgerTransactionEntity
        {
            LedgerTransactionId = reversalId,
            PeriodId = deposit.PeriodId,
            BuildingId = deposit.BuildingId,
            Kind = LedgerTransactionKind.Reversal,
            Status = LedgerTransactionStatus.Posted,
            IdempotencyKey = $"payment-deposit:{deposit.PaymentDepositId:D}:reverse",
            SourceSystem = deposit.SourceSystem,
            SourceType = "PaymentDepositReversal",
            SourceKey = deposit.PaymentDepositId.ToString("D"),
            ReversesTransactionId = original.LedgerTransactionId,
            ActorAccountId = actorAccountId,
            ActorReference = actorAccountId.ToString("D"),
            Reason = reason.Trim(),
            CreatedAtUtc = now,
            PostedAtUtc = now
        });
        db.LedgerEntries.Add(new LedgerEntryEntity
        {
            LedgerEntryId = Guid.NewGuid(),
            LedgerTransactionId = reversalId,
            PeriodId = deposit.PeriodId,
            BuildingId = deposit.BuildingId,
            ApartmentId = deposit.ApartmentId,
            EntryKind = LedgerEntryKind.Reversal,
            SignedAmount = -originalEntries[0].SignedAmount,
            CurrencyCode = deposit.CurrencyCode,
            Description = reason.Trim(),
            EffectiveAtUtc = now,
            CreatedAtUtc = now
        });

        deposit.Status = PaymentDepositStatus.Reversed;
        deposit.ReversedAtUtc = now;
        deposit.ReversedByAccountId = actorAccountId;
        deposit.ReversalTransactionId = reversalId;
        deposit.ResolutionReason = reason.Trim();
        db.AuditEvents.Add(CreateAuditEvent(
            deposit.BuildingId, actorAccountId, "PaymentDepositReversed", deposit.ApartmentId, now,
            new { paymentDepositId = deposit.PaymentDepositId, amount = deposit.Amount, currencyCode = deposit.CurrencyCode, reversalId, reason = reason.Trim() }));
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return reversalId;
    }

    /// <summary>Cancels a pending deposit submission with the supplied reason.</summary>
    public async Task<bool> CancelAsync(
        Guid paymentDepositId,
        Guid actorAccountId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (paymentDepositId == Guid.Empty) throw new ArgumentException("Deposit id is required.", nameof(paymentDepositId));
        if (actorAccountId == Guid.Empty) throw new ArgumentException("An authenticated actor is required.", nameof(actorAccountId));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("A cancellation reason of 1 to 500 characters is required.", nameof(reason));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var deposit = await db.PaymentDeposits.SingleOrDefaultAsync(
            d => d.PaymentDepositId == paymentDepositId, cancellationToken);
        if (deposit is null) return false;
        if (deposit.Status != PaymentDepositStatus.Pending)
            throw new InvalidOperationException("Only a pending deposit can be cancelled.");

        var actorIsActive = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == actorAccountId && account.AccountStatus == 1 && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);
        if (!actorIsActive)
            throw new InvalidOperationException("An active target account is required to cancel a deposit.");
        var authorizationTime = _timeProvider.GetUtcNow().UtcDateTime;
        if (!await HasFinanceManagementRoleAsync(db, actorAccountId, deposit.BuildingId, authorizationTime, cancellationToken))
            throw new InvalidOperationException("The actor has no active finance-management assignment for this building.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        deposit.Status = PaymentDepositStatus.Cancelled;
        deposit.CancelledAtUtc = now;
        deposit.CancelledByAccountId = actorAccountId;
        deposit.ResolutionReason = reason.Trim();
        db.AuditEvents.Add(CreateAuditEvent(
            deposit.BuildingId, actorAccountId, "PaymentDepositCancelled", deposit.ApartmentId, now,
            new { paymentDepositId = deposit.PaymentDepositId, reason = reason.Trim() }));
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    private static Task<bool> HasScopedRoleAsync(
        AppDbContext db,
        Guid accountId,
        string roleKey,
        string buildingId,
        string apartmentId,
        DateTime now,
        CancellationToken cancellationToken) =>
        (from assignment in db.UserRoleAssignments.AsNoTracking()
         join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
         where assignment.AccountId == accountId && assignment.RoleKey == roleKey && role.IsActive &&
             assignment.ValidFromUtc <= now &&
             (assignment.ValidToUtc == null || assignment.ValidToUtc > now) &&
             assignment.RevokedAtUtc == null &&
             ((assignment.ScopeType == 1 && assignment.ScopeBuildingId == buildingId &&
               assignment.ScopeApartmentId == null) ||
              (assignment.ScopeType == 2 && assignment.ScopeBuildingId == buildingId &&
               assignment.ScopeApartmentId == apartmentId))
         select assignment.RoleAssignmentId).AnyAsync(cancellationToken);

    private static Task<bool> HasFinanceManagementRoleAsync(
        AppDbContext db,
        Guid accountId,
        string buildingId,
        DateTime now,
        CancellationToken cancellationToken) =>
        (from assignment in db.UserRoleAssignments.AsNoTracking()
         join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
         where assignment.AccountId == accountId && role.IsActive &&
             (assignment.RoleKey == "building-admin" || assignment.RoleKey == "superadmin") &&
             assignment.ValidFromUtc <= now &&
             (assignment.ValidToUtc == null || assignment.ValidToUtc > now) &&
             assignment.RevokedAtUtc == null &&
             ((assignment.RoleKey == "building-admin" && assignment.ScopeType == 1 &&
               assignment.ScopeBuildingId == buildingId && assignment.ScopeApartmentId == null) ||
              (assignment.RoleKey == "superadmin" && assignment.ScopeType == 0 &&
               assignment.ScopeBuildingId == null && assignment.ScopeApartmentId == null))
         select assignment.RoleAssignmentId).AnyAsync(cancellationToken);

    private static AuditEventEntity CreateAuditEvent(
        string buildingId,
        Guid actorAccountId,
        string eventType,
        string apartmentId,
        DateTime occurredAtUtc,
        object details) => new()
    {
        AuditEventId = Guid.NewGuid(),
        OccurredAtUtc = occurredAtUtc,
        ActorAccountId = actorAccountId,
        ActorReference = actorAccountId.ToString("D"),
        BuildingId = buildingId,
        EventCategory = "Finance",
        EventType = eventType,
        ResourceType = "Apartment",
        ResourceReference = apartmentId,
        Outcome = 0,
        SafeDetailsJson = JsonSerializer.Serialize(details)
    };
}
