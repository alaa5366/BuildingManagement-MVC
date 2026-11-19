using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Writes expense and revenue commands directly to the target finance schema.
/// Source row, allocation evidence, ledger entries and audit event commit atomically.
/// </summary>
public sealed class TargetExpenseRevenueWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TimeProvider _timeProvider;

    public TargetExpenseRevenueWorkflowService(
        IDbContextFactory<AppDbContext> factory,
        TimeProvider timeProvider)
    {
        _factory = factory;
        _timeProvider = timeProvider;
    }

    public Task<Guid> AddExpenseAsync(
        string buildingId, string monthKey, string categoryId, string note,
        decimal amount, string transactionDate, Guid commandId, Guid actorAccountId,
        CancellationToken cancellationToken = default) =>
        AddAsync(false, buildingId, monthKey, categoryId, note, amount,
            transactionDate, commandId, actorAccountId, cancellationToken);

    public Task<Guid> AddRevenueAsync(
        string buildingId, string monthKey, string categoryId, string note,
        decimal amount, string transactionDate, Guid commandId, Guid actorAccountId,
        CancellationToken cancellationToken = default) =>
        AddAsync(true, buildingId, monthKey, categoryId, note, amount,
            transactionDate, commandId, actorAccountId, cancellationToken);

    private async Task<Guid> AddAsync(
        bool isRevenue, string buildingId, string monthKey, string categoryId,
        string note, decimal amount, string transactionDate, Guid commandId,
        Guid actorAccountId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || buildingId.Length > 64)
            throw new ArgumentException("A valid building is required.", nameof(buildingId));
        if (commandId == Guid.Empty) throw new ArgumentException("A command id is required.", nameof(commandId));
        if (actorAccountId == Guid.Empty) throw new ArgumentException("An authenticated actor is required.", nameof(actorAccountId));
        if (string.IsNullOrWhiteSpace(categoryId) || categoryId.Length > 64)
            throw new ArgumentException("A valid financial category is required.", nameof(categoryId));
        if (string.IsNullOrWhiteSpace(note) || note.Trim().Length > 500)
            throw new ArgumentException("A note of 1 to 500 characters is required.", nameof(note));
        if (amount <= 0m || decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive and precise to cents.");
        if (!DateOnly.TryParseExact(monthKey + "-01", "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _))
            throw new ArgumentException("Month must use exact yyyy-MM format.", nameof(monthKey));
        if (!DateOnly.TryParseExact(transactionDate, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var effectiveDate) ||
            effectiveDate.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture) != monthKey)
            throw new ArgumentException("Transaction date must be an exact date within the selected month.", nameof(transactionDate));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var sourceType = isRevenue ? "Revenue" : "Expense";
        var sourceKey = commandId.ToString("D");
        var idempotencyKey = $"finance:{sourceType.ToLowerInvariant()}:{sourceKey}";

        var existing = await db.LedgerTransactions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var sameSource = isRevenue
                ? await db.Revenues.AsNoTracking().SingleOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == commandId.ToString("N"), cancellationToken)
                : null;
            var sameExpense = !isRevenue
                ? await db.Expenses.AsNoTracking().SingleOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == commandId.ToString("N"), cancellationToken)
                : null;
            var samePayload = isRevenue
                ? sameSource is not null && sameSource.MonthKey == monthKey && sameSource.CategoryId == categoryId &&
                  sameSource.Amount == amount && sameSource.Note == note.Trim() && sameSource.RevenueDate == effectiveDate
                : sameExpense is not null && sameExpense.MonthKey == monthKey && sameExpense.CategoryId == categoryId &&
                  sameExpense.Amount == amount && sameExpense.Note == note.Trim() && sameExpense.ExpenseDate == effectiveDate;
            if (existing.BuildingId != buildingId || existing.Kind != (isRevenue ? LedgerTransactionKind.Revenue : LedgerTransactionKind.Expense) ||
                existing.Status != LedgerTransactionStatus.Posted || existing.SourceSystem != "application" ||
                existing.SourceType != sourceType || existing.SourceKey != sourceKey || !samePayload)
                throw new InvalidOperationException("The command id was already used by a different finance payload.");
            await tx.CommitAsync(cancellationToken);
            return existing.LedgerTransactionId;
        }

        var activeActor = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == actorAccountId && account.AccountStatus == 1 && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);
        if (!activeActor) throw new InvalidOperationException("An active target account is required.");

        var assignedAdmin = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
            where assignment.AccountId == actorAccountId && assignment.RoleKey == "building-admin" &&
                role.IsActive && assignment.ScopeType == 1 &&
                assignment.ScopeBuildingId == buildingId && assignment.ScopeApartmentId == null &&
                assignment.ValidFromUtc <= now &&
                (assignment.ValidToUtc == null || assignment.ValidToUtc > now) &&
                assignment.RevokedAtUtc == null
            select assignment.RoleAssignmentId).AnyAsync(cancellationToken);
        if (!assignedAdmin)
            throw new InvalidOperationException("Only an active building-scoped Admin can post expenses or revenues.");

        var buildingSettings = await db.BuildingSettings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.BuildingId == buildingId, cancellationToken);
        var currencyCode = string.IsNullOrWhiteSpace(buildingSettings?.Currency)
            ? "EGP"
            : buildingSettings.Currency.Trim().ToUpperInvariant();
        if (currencyCode.Length != 3 || !currencyCode.All(char.IsAsciiLetter))
            throw new InvalidOperationException("The building currency configuration is invalid.");

        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            x => x.BuildingId == buildingId && x.PeriodKey == monthKey, cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("The selected financial period is unavailable or closed.");

        TimeZoneInfo buildingTimeZone;
        try
        {
            buildingTimeZone = TimeZoneInfo.FindSystemTimeZoneById(period.TimeZoneId);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new InvalidOperationException("The building financial period has an unknown time zone.", ex);
        }
        var localEffectiveTime = DateTime.SpecifyKind(effectiveDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        if (buildingTimeZone.IsInvalidTime(localEffectiveTime))
            throw new InvalidOperationException("The selected date begins at an invalid local time in the building time zone.");
        var effectiveAtUtc = TimeZoneInfo.ConvertTimeToUtc(localEffectiveTime, buildingTimeZone);
        if (effectiveAtUtc < period.StartsAtUtc || effectiveAtUtc >= period.EndsAtUtc)
            throw new InvalidOperationException("The transaction date is outside the selected financial period.");

        var category = await db.FinancialCategories.AsNoTracking().SingleOrDefaultAsync(
            x => x.BuildingId == buildingId && x.Id == categoryId &&
                 x.IsActive && x.Kind == (isRevenue ? "revenue" : "expense"), cancellationToken);
        if (category is null)
            throw new InvalidOperationException("The financial category is missing, inactive, or has the wrong type.");

        var apartments = await db.Apartments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && !x.IsClosed)
            .Select(x => x.Id).OrderBy(x => x).ToListAsync(cancellationToken);
        var allocation = FinancialAllocationCalculator.AllocateEqually(amount, apartments);
        var transactionId = Guid.NewGuid();
        var sourceId = commandId.ToString("N");
        var description = note.Trim();
        var entryKind = isRevenue ? LedgerEntryKind.RevenueAllocation : LedgerEntryKind.ExpenseAllocation;
        var signedFactor = isRevenue ? 1m : -1m;

        db.LedgerTransactions.Add(new LedgerTransactionEntity
        {
            LedgerTransactionId = transactionId,
            PeriodId = period.PeriodId,
            BuildingId = buildingId,
            Kind = isRevenue ? LedgerTransactionKind.Revenue : LedgerTransactionKind.Expense,
            Status = LedgerTransactionStatus.Posted,
            IdempotencyKey = idempotencyKey,
            SourceSystem = "application",
            SourceType = sourceType,
            SourceKey = sourceKey,
            ActorAccountId = actorAccountId,
            ActorReference = actorAccountId.ToString("D"),
            Reason = description,
            CreatedAtUtc = now,
            PostedAtUtc = now
        });

        foreach (var share in allocation)
        {
            db.LedgerEntries.Add(new LedgerEntryEntity
            {
                LedgerEntryId = Guid.NewGuid(),
                LedgerTransactionId = transactionId,
                PeriodId = period.PeriodId,
                BuildingId = buildingId,
                ApartmentId = share.ApartmentId,
                EntryKind = entryKind,
                SignedAmount = share.AllocatedAmount * signedFactor,
                CurrencyCode = currencyCode,
                Description = description,
                EffectiveAtUtc = effectiveAtUtc,
                CreatedAtUtc = now
            });
        }

        var fingerprintPayload = string.Join("|", allocation.Select(x =>
            $"{x.ApartmentId}:{x.AllocatedAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}"));
        var fingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{sourceType}|{buildingId}|{monthKey}|{categoryId}|{amount:0.00}|{fingerprintPayload}"));

        var batchId = Guid.NewGuid();
        db.FinancialAllocationBatches.Add(new FinancialAllocationBatchEntity
        {
            AllocationBatchId = batchId,
            PeriodId = period.PeriodId,
            BuildingId = buildingId,
            SourceTransactionId = transactionId,
            BasisKey = "all-open-apartments-equal",
            AlgorithmVersion = FinancialAllocationCalculator.AlgorithmVersion,
            InputFingerprint = fingerprint,
            SourceAmount = amount,
            AllocatedAmount = allocation.Sum(x => x.AllocatedAmount),
            CurrencyCode = currencyCode,
            Status = FinancialAllocationStatus.Completed,
            CreatedAtUtc = now,
            CompletedAtUtc = now,
            CreatedByAccountId = actorAccountId
        });
        foreach (var share in allocation)
        {
            db.FinancialAllocationLines.Add(new FinancialAllocationLineEntity
            {
                AllocationLineId = Guid.NewGuid(),
                AllocationBatchId = batchId,
                PeriodId = period.PeriodId,
                BuildingId = buildingId,
                ApartmentId = share.ApartmentId,
                Weight = share.Weight,
                ExactShare = share.ExactShare,
                AllocatedAmount = share.AllocatedAmount,
                RemainderRank = share.RemainderRank
            });
        }

        if (isRevenue)
        {
            db.Revenues.Add(new RevenueEntity
            {
                BuildingId = buildingId, Id = sourceId, MonthKey = monthKey,
                CategoryId = categoryId, CategoryKind = "revenue", Note = description,
                Amount = amount, RevenueDate = effectiveDate
            });
        }
        else
        {
            db.Expenses.Add(new ExpenseEntity
            {
                BuildingId = buildingId, Id = sourceId, MonthKey = monthKey,
                CategoryId = categoryId, CategoryKind = "expense", Note = description,
                Amount = amount, ExpenseDate = effectiveDate
            });
        }

        db.FinancialSourceLinks.Add(new FinancialSourceLinkEntity
        {
            FinancialSourceLinkId = Guid.NewGuid(),
            SourceSystem = "application",
            SourceType = sourceType,
            SourceKey = sourceKey,
            LedgerTransactionId = transactionId,
            SourceFingerprint = fingerprint,
            LinkedAtUtc = now
        });
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            OccurredAtUtc = now,
            ActorAccountId = actorAccountId,
            ActorReference = actorAccountId.ToString("D"),
            BuildingId = buildingId,
            EventCategory = "Finance",
            EventType = isRevenue ? "RevenuePosted" : "ExpensePosted",
            ResourceType = sourceType,
            ResourceReference = sourceKey,
            Outcome = 0,
            SafeDetailsJson = JsonSerializer.Serialize(new
            {
                amount, currencyCode, monthKey, categoryId,
                ledgerTransactionId = transactionId, allocationCount = allocation.Count,
                algorithm = FinancialAllocationCalculator.AlgorithmVersion
            })
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return transactionId;
    }
    /// <summary>
    /// Reverses a posted expense or revenue without deleting its source evidence.
    /// The reversal is recorded in the currently open building period.
    /// </summary>
    public async Task<Guid> ReverseAsync(
        bool isRevenue, string buildingId, string sourceId, string reason,
        Guid commandId, Guid actorAccountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId)) throw new ArgumentException("Building is required.", nameof(buildingId));
        if (!Guid.TryParseExact(sourceId, "N", out var sourceGuid) &&
            !Guid.TryParse(sourceId, out sourceGuid))
            throw new ArgumentException("A valid source transaction id is required.", nameof(sourceId));
        if (commandId == Guid.Empty || actorAccountId == Guid.Empty)
            throw new ArgumentException("Command and actor ids are required.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("A reversal reason of 1 to 500 characters is required.", nameof(reason));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var sourceType = isRevenue ? "Revenue" : "Expense";
        var sourceKey = sourceGuid.ToString("D");
        var original = await db.LedgerTransactions.SingleOrDefaultAsync(x =>
            x.BuildingId == buildingId && x.SourceSystem == "application" &&
            x.SourceType == sourceType && x.SourceKey == sourceKey &&
            x.Kind == (isRevenue ? LedgerTransactionKind.Revenue : LedgerTransactionKind.Expense) &&
            x.Status == LedgerTransactionStatus.Posted, cancellationToken);
        if (original is null) throw new InvalidOperationException("The posted finance source was not found.");

        var reversalKey = $"finance:reverse:{sourceType.ToLowerInvariant()}:{sourceKey}";
        var existing = await db.LedgerTransactions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == reversalKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.BuildingId != buildingId || existing.ReversesTransactionId != original.LedgerTransactionId ||
                existing.Status != LedgerTransactionStatus.Posted)
                throw new InvalidOperationException("A conflicting reversal already exists.");
            await tx.CommitAsync(cancellationToken);
            return existing.LedgerTransactionId;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activeActor = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == actorAccountId && account.AccountStatus == 1 && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);
        if (!activeActor) throw new InvalidOperationException("An active target account is required.");
        var assignedAdmin = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
            where assignment.AccountId == actorAccountId && assignment.RoleKey == "building-admin" &&
                role.IsActive && assignment.ScopeType == 1 && assignment.ScopeBuildingId == buildingId &&
                assignment.ScopeApartmentId == null && assignment.ValidFromUtc <= now &&
                (assignment.ValidToUtc == null || assignment.ValidToUtc > now) && assignment.RevokedAtUtc == null
            select assignment.RoleAssignmentId).AnyAsync(cancellationToken);
        if (!assignedAdmin) throw new InvalidOperationException("Only an active building Admin can reverse finance postings.");

        var currentMonth = now.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            x => x.BuildingId == buildingId && x.PeriodKey == currentMonth, cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed ||
            now < period.StartsAtUtc || now >= period.EndsAtUtc)
            throw new InvalidOperationException("A reversal requires the current open financial period.");

        var originalEntries = await db.LedgerEntries.AsNoTracking()
            .Where(x => x.LedgerTransactionId == original.LedgerTransactionId &&
                        x.BuildingId == buildingId && x.PeriodId == original.PeriodId)
            .OrderBy(x => x.ApartmentId).ThenBy(x => x.LedgerEntryId)
            .ToListAsync(cancellationToken);
        if (originalEntries.Count == 0 || originalEntries.Any(x => x.SignedAmount == 0m))
            throw new InvalidOperationException("The original ledger entries are incomplete.");

        var reversalId = Guid.NewGuid();
        db.LedgerTransactions.Add(new LedgerTransactionEntity
        {
            LedgerTransactionId = reversalId, PeriodId = period.PeriodId, BuildingId = buildingId,
            Kind = LedgerTransactionKind.Reversal, Status = LedgerTransactionStatus.Posted,
            IdempotencyKey = reversalKey, SourceSystem = "application",
            SourceType = sourceType + "Reversal", SourceKey = sourceKey,
            ReversesTransactionId = original.LedgerTransactionId, ActorAccountId = actorAccountId,
            ActorReference = actorAccountId.ToString("D"), Reason = reason.Trim(),
            CreatedAtUtc = now, PostedAtUtc = now
        });

        foreach (var entry in originalEntries)
            db.LedgerEntries.Add(new LedgerEntryEntity
            {
                LedgerEntryId = Guid.NewGuid(), LedgerTransactionId = reversalId,
                PeriodId = period.PeriodId, BuildingId = buildingId, ApartmentId = entry.ApartmentId,
                EntryKind = LedgerEntryKind.Reversal, SignedAmount = -entry.SignedAmount,
                CurrencyCode = entry.CurrencyCode, Description = reason.Trim(),
                EffectiveAtUtc = now, CreatedAtUtc = now
            });

        db.FinancialSourceLinks.Add(new FinancialSourceLinkEntity
        {
            FinancialSourceLinkId = Guid.NewGuid(), SourceSystem = "application",
            SourceType = sourceType + "Reversal", SourceKey = commandId.ToString("D"),
            LedgerTransactionId = reversalId, LinkedAtUtc = now
        });
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(), OccurredAtUtc = now, ActorAccountId = actorAccountId,
            ActorReference = actorAccountId.ToString("D"), BuildingId = buildingId,
            EventCategory = "Finance", EventType = sourceType + "Reversed",
            ResourceType = sourceType, ResourceReference = sourceKey, Outcome = 0,
            SafeDetailsJson = JsonSerializer.Serialize(new
            {
                originalTransactionId = original.LedgerTransactionId,
                reversalTransactionId = reversalId, reason = reason.Trim(),
                entryCount = originalEntries.Count
            })
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return reversalId;
    }

}
