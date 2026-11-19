using System.Data;
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

    public async Task<Guid> CreatePendingAsync(
        string buildingId,
        string apartmentId,
        Guid periodId,
        decimal amount,
        string currencyCode = "EGP",
        string? sourceKey = null,
        CancellationToken cancellationToken = default)
    {
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

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            p => p.PeriodId == periodId && p.BuildingId == buildingId, cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("An open financial period for this building is required.");

        var apartmentExists = await db.Apartments.AsNoTracking()
            .AnyAsync(a => a.BuildingId == buildingId && a.Id == apartmentId, cancellationToken);
        if (!apartmentExists)
            throw new InvalidOperationException("The apartment does not belong to this building.");

        if (sourceKey is not null)
        {
            var existing = await db.PaymentDeposits.AsNoTracking().SingleOrDefaultAsync(
                d => d.SourceSystem == "application" && d.SourceKey == sourceKey, cancellationToken);
            if (existing is not null)
            {
                if (existing.BuildingId == buildingId && existing.ApartmentId == apartmentId &&
                    existing.PeriodId == periodId && existing.Amount == amount &&
                    existing.CurrencyCode == currencyCode.ToUpperInvariant())
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
            SubmittedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            SourceSystem = "application",
            SourceKey = sourceKey
        });
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return id;
    }

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
            await tx.CommitAsync(cancellationToken);
            return deposit.PostedTransactionId;
        }
        if (deposit.Status != PaymentDepositStatus.Pending)
            throw new InvalidOperationException("Only a pending deposit can be confirmed.");

        var actorIsActive = await db.UserAccounts.AsNoTracking()
            .AnyAsync(a => a.AccountId == actorAccountId && a.AccountStatus == 1, cancellationToken);
        if (!actorIsActive)
            throw new InvalidOperationException("An active target account is required to confirm a deposit.");

        var period = await db.FinancialPeriods.SingleOrDefaultAsync(
            p => p.PeriodId == deposit.PeriodId && p.BuildingId == deposit.BuildingId, cancellationToken);
        if (period is null || period.Status == FinancialPeriodStatus.Closed)
            throw new InvalidOperationException("The deposit financial period is closed or unavailable.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (now < period.StartsAtUtc || now >= period.EndsAtUtc)
            throw new InvalidOperationException("The deposit cannot be posted outside its financial period.");

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
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return transactionId;
    }

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

        var actorIsActive = await db.UserAccounts.AsNoTracking()
            .AnyAsync(a => a.AccountId == actorAccountId && a.AccountStatus == 1, cancellationToken);
        if (!actorIsActive)
            throw new InvalidOperationException("An active target account is required to cancel a deposit.");

        deposit.Status = PaymentDepositStatus.Cancelled;
        deposit.CancelledAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        deposit.CancelledByAccountId = actorAccountId;
        deposit.ResolutionReason = reason.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }
}
