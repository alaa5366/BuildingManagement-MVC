using System.Data;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>One evidence-backed person share within an apartment ownership period.</summary>
public sealed record OwnershipShareInput(
    Guid PersonId,
    decimal SharePercent,
    string EvidenceReference,
    string? Notes = null);

/// <summary>Atomic batch describing all owner rows in one effective period.</summary>
public sealed record RecordOwnershipPeriodRequest(
    string BuildingId,
    string ApartmentId,
    DateTime ValidFromUtc,
    DateTime? ValidToUtc,
    OwnershipCompletenessStatus CompletenessStatus,
    DateTime? CompletenessVerifiedAtUtc,
    Guid? CompletenessVerifiedByAccountId,
    OwnershipRecordSource RecordSource,
    Guid? RecordedByAccountId,
    IReadOnlyList<OwnershipShareInput> Owners,
    IReadOnlyCollection<Guid>? CloseExistingOwnershipIds = null,
    string? ClosureReason = null);

/// <summary>Persists evidence-backed ownership periods with serializable share validation.</summary>
public sealed class ApartmentOwnershipService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the service using the configured database factory and UTC clock.</summary>
    public ApartmentOwnershipService(IDbContextFactory<AppDbContext> dbFactory, TimeProvider timeProvider)
    {
        _dbFactory = dbFactory;
        _timeProvider = timeProvider;
    }

    /// <summary>Records a whole effective-dated ownership set atomically.</summary>
    /// <param name="request">The period, completeness assertion, source, and all owner shares.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The identifiers of the created ownership rows.</returns>
    public async Task<IReadOnlyList<Guid>> RecordPeriodAsync(
        RecordOwnershipPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        OwnershipPeriodValidator.ValidateRequest(request, nowUtc);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var apartmentExists = await db.Apartments.AsNoTracking().AnyAsync(
            item => item.BuildingId == request.BuildingId && item.Id == request.ApartmentId,
            cancellationToken);
        if (!apartmentExists)
            throw new InvalidOperationException("The requested apartment does not exist.");

        await EnsureActiveAccountAsync(db, request.RecordedByAccountId, "record actor", cancellationToken);
        await EnsureActiveAccountAsync(db, request.CompletenessVerifiedByAccountId, "completeness reviewer", cancellationToken);

        var ownerPersonIds = request.Owners.Select(item => item.PersonId).Distinct().ToArray();
        var knownOwnerCount = await db.Persons.CountAsync(item => ownerPersonIds.Contains(item.PersonId) && !item.IsArchived, cancellationToken);
        if (knownOwnerCount != ownerPersonIds.Length)
            throw new InvalidOperationException("Every owner must be an active, known person.");

        var closeIds = (request.CloseExistingOwnershipIds ?? Array.Empty<Guid>()).Distinct().ToArray();
        if (closeIds.Length > 0 && string.IsNullOrWhiteSpace(request.ClosureReason))
            throw new ArgumentException("Closing ownership rows requires a reason.", nameof(request));
        if (request.ClosureReason?.Trim().Length > 500)
            throw new ArgumentException("The closure reason exceeds the supported length.", nameof(request));
        var rowsToClose = closeIds.Length == 0
            ? new List<ApartmentOwnershipEntity>()
            : await db.ApartmentOwnerships.Where(item => closeIds.Contains(item.OwnershipId)
                && item.BuildingId == request.BuildingId
                && item.ApartmentId == request.ApartmentId).ToListAsync(cancellationToken);

        if (rowsToClose.Count != closeIds.Length || rowsToClose.Any(item =>
                item.ValidFromUtc >= request.ValidFromUtc ||
                (item.ValidToUtc is not null && item.ValidToUtc <= request.ValidFromUtc)))
            throw new InvalidOperationException("The ownership period cannot close one or more selected rows at its start time.");

        var overlapQuery = db.ApartmentOwnerships.Where(item =>
            item.BuildingId == request.BuildingId &&
            item.ApartmentId == request.ApartmentId &&
            item.ValidFromUtc < (request.ValidToUtc ?? DateTime.MaxValue) &&
            (item.ValidToUtc == null || item.ValidToUtc > request.ValidFromUtc));
        if (closeIds.Length > 0)
            overlapQuery = overlapQuery.Where(item => !closeIds.Contains(item.OwnershipId));

        var overlappingRows = await overlapQuery.ToListAsync(cancellationToken);
        OwnershipPeriodValidator.ValidateAgainstExisting(request, overlappingRows, nowUtc);

        foreach (var row in rowsToClose)
            row.ValidToUtc = request.ValidFromUtc;

        var created = request.Owners.Select(owner => new ApartmentOwnershipEntity
        {
            OwnershipId = Guid.NewGuid(),
            BuildingId = request.BuildingId,
            ApartmentId = request.ApartmentId,
            PersonId = owner.PersonId,
            OwnershipSharePercent = owner.SharePercent,
            CompletenessStatus = request.CompletenessStatus,
            RecordSource = request.RecordSource,
            ValidFromUtc = request.ValidFromUtc,
            ValidToUtc = request.ValidToUtc,
            RecordedAtUtc = nowUtc,
            RecordedByAccountId = request.RecordedByAccountId,
            EvidenceReference = owner.EvidenceReference.Trim(),
            CompletenessVerifiedAtUtc = request.CompletenessVerifiedAtUtc,
            CompletenessVerifiedByAccountId = request.CompletenessVerifiedByAccountId,
            Notes = string.IsNullOrWhiteSpace(owner.Notes) ? null : owner.Notes.Trim()
        }).ToList();

        db.ApartmentOwnerships.AddRange(created);
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            OccurredAtUtc = nowUtc,
            ActorAccountId = request.RecordedByAccountId,
            ActorReference = request.RecordedByAccountId?.ToString("D") ?? "system",
            BuildingId = request.BuildingId,
            EventCategory = "ApartmentOwnership",
            EventType = "ownership.period.recorded",
            ResourceType = "ApartmentOwnershipPeriod",
            ResourceReference = request.ApartmentId,
            Outcome = 0,
            SafeDetailsJson = JsonSerializer.Serialize(new
            {
                ownershipIds = created.Select(item => item.OwnershipId),
                closedOwnershipIds = rowsToClose.Select(item => item.OwnershipId),
                closureReason = request.ClosureReason?.Trim(),
                request.ValidFromUtc,
                request.ValidToUtc,
                request.CompletenessStatus,
                ownerCount = created.Count,
                totalSharePercent = created.Sum(item => item.OwnershipSharePercent)
            })
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return created.Select(item => item.OwnershipId).ToArray();
    }

    private static async Task EnsureActiveAccountAsync(
        AppDbContext db,
        Guid? accountId,
        string fieldName,
        CancellationToken cancellationToken)
    {
        if (accountId is null)
            return;

        var active = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == accountId.Value
                  && account.AccountStatus == 1
                  && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);

        if (!active)
            throw new InvalidOperationException($"The {fieldName} must be an active account.");
    }
}

/// <summary>Validates period-wide completeness and overlapping ownership share invariants.</summary>
internal static class OwnershipPeriodValidator
{
    internal static void ValidateRequest(RecordOwnershipPeriodRequest request, DateTime nowUtc)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.BuildingId) || string.IsNullOrWhiteSpace(request.ApartmentId))
            throw new ArgumentException("Building and apartment identifiers are required.", nameof(request));
        if (request.ValidFromUtc.Kind != DateTimeKind.Utc ||
            (request.ValidToUtc is not null && request.ValidToUtc.Value.Kind != DateTimeKind.Utc) ||
            request.ValidToUtc <= request.ValidFromUtc)
            throw new ArgumentException("Ownership dates must be UTC and the end must be after the start.", nameof(request));
        if (!Enum.IsDefined(request.CompletenessStatus) || !Enum.IsDefined(request.RecordSource))
            throw new ArgumentException("The ownership status or source is unknown.", nameof(request));
        if (request.Owners is null || request.Owners.Count == 0)
            throw new ArgumentException("At least one owner share is required.", nameof(request));
        if (request.Owners.Any(item => item.PersonId == Guid.Empty ||
                item.SharePercent <= 0m || item.SharePercent > 100m ||
                string.IsNullOrWhiteSpace(item.EvidenceReference) || item.EvidenceReference.Trim().Length > 500))
            throw new ArgumentException("Every share needs a person, a percentage between 0 and 100, and evidence.", nameof(request));
        if (request.Owners.Select(item => item.PersonId).Distinct().Count() != request.Owners.Count)
            throw new ArgumentException("A person may appear only once in an ownership period.", nameof(request));
        if (request.Owners.Any(item => item.Notes?.Length > 1000))
            throw new ArgumentException("Ownership notes exceed the supported length.", nameof(request));
        if (request.RecordSource == OwnershipRecordSource.Human && request.RecordedByAccountId is null)
            throw new ArgumentException("Human-entered ownership records require an actor account.", nameof(request));
        if (request.RecordSource != OwnershipRecordSource.Human && request.RecordedByAccountId is not null && request.RecordedByAccountId == Guid.Empty)
            throw new ArgumentException("A supplied actor account identifier cannot be empty.", nameof(request));

        var verified = request.CompletenessStatus == OwnershipCompletenessStatus.CompleteVerified;
        var hasVerificationMetadata = request.CompletenessVerifiedAtUtc is not null && request.CompletenessVerifiedByAccountId is not null;
        var hasPartialVerificationMetadata = (request.CompletenessVerifiedAtUtc is null) != (request.CompletenessVerifiedByAccountId is null);
        if (hasPartialVerificationMetadata || verified != hasVerificationMetadata)
            throw new ArgumentException("CompleteVerified periods require a reviewer and timestamp; other states must not carry them.", nameof(request));
        if (request.CompletenessVerifiedAtUtc is not null &&
            (request.CompletenessVerifiedAtUtc.Value.Kind != DateTimeKind.Utc ||
             request.CompletenessVerifiedAtUtc.Value > nowUtc))
            throw new ArgumentException("The completeness verification time must be a past or current UTC time.", nameof(request));

        var total = request.Owners.Sum(item => item.SharePercent);
        if (total > 100m)
            throw new InvalidOperationException("Ownership shares cannot exceed 100 percent.");
        if (verified && total != 100m)
            throw new InvalidOperationException("A CompleteVerified period must total exactly 100 percent.");
    }

    internal static void ValidateAgainstExisting(
        RecordOwnershipPeriodRequest request,
        IReadOnlyCollection<ApartmentOwnershipEntity> overlappingRows,
        DateTime nowUtc)
    {
        ValidateRequest(request, nowUtc);

        if (overlappingRows.Any(item => item.CompletenessStatus != request.CompletenessStatus))
            throw new InvalidOperationException("Overlapping ownership rows must use the same completeness status.");
        if (request.CompletenessStatus == OwnershipCompletenessStatus.CompleteVerified && overlappingRows.Count != 0)
            throw new InvalidOperationException("A verified complete ownership period cannot overlap another ownership period.");

        var existingPersonIds = overlappingRows.Select(item => item.PersonId).ToHashSet();
        if (request.Owners.Any(item => existingPersonIds.Contains(item.PersonId)))
            throw new InvalidOperationException("A person cannot have overlapping ownership rows for the same apartment.");

        var boundaries = new SortedSet<DateTime> { request.ValidFromUtc };
        foreach (var row in overlappingRows)
        {
            if (row.ValidFromUtc > request.ValidFromUtc &&
                (request.ValidToUtc is null || row.ValidFromUtc < request.ValidToUtc.Value))
                boundaries.Add(row.ValidFromUtc);
            if (row.ValidToUtc is not null && row.ValidToUtc > request.ValidFromUtc &&
                (request.ValidToUtc is null || row.ValidToUtc < request.ValidToUtc.Value))
                boundaries.Add(row.ValidToUtc.Value);
        }

        foreach (var point in boundaries)
        {
            var requestActive = request.ValidToUtc is null || point < request.ValidToUtc.Value;
            if (!requestActive)
                continue;

            var existingTotal = overlappingRows
                .Where(item => item.ValidFromUtc <= point && (item.ValidToUtc is null || item.ValidToUtc > point))
                .Sum(item => item.OwnershipSharePercent);
            var total = existingTotal + request.Owners.Sum(item => item.SharePercent);
            if (total > 100m)
                throw new InvalidOperationException("Overlapping ownership shares would exceed 100 percent.");
        }
    }
}
