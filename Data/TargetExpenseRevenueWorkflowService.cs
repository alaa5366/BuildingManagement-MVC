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

        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            x => x.BuildingId == buildingId && x.PeriodKey == monthKey, cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("The selected financial period is unavailable or closed.");

        var effectiveAtUtc = DateTime.SpecifyKind(effectiveDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
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
                CurrencyCode = "EGP",
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
            CurrencyCode = "EGP",
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
                amount, currencyCode = "EGP", monthKey, categoryId,
                ledgerTransactionId = transactionId, allocationCount = allocation.Count,
                algorithm = FinancialAllocationCalculator.AlgorithmVersion
            })
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return transactionId;
    }
}
