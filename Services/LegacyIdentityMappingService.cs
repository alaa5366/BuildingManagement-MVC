using System.Data;
using System.Security.Claims;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Human-reviewed dispositions supported by the legacy identity map.</summary>
public enum LegacyIdentityMappingStatus : byte
{
    Pending = 0,
    Linked = 1,
    Separate = 2,
    Rejected = 3,
    ManualReview = 4
}

/// <summary>Reviewed target identity decision for one legacy source record.</summary>
public sealed record ReviewLegacyIdentityMappingRequest(
    Guid MappingId,
    LegacyIdentityMappingStatus Status,
    Guid? PersonId,
    Guid? AccountId,
    string? MatchMethod,
    string? EvidenceReference,
    string ReviewReason);

/// <summary>Registers and reviews explicit legacy identity dispositions without fuzzy matching.</summary>
public sealed class LegacyIdentityMappingService
{
    private const string SourceSystem = "legacy-sql";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthorizationEvaluator _authorizationEvaluator;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the mapping service with persistence, authorization, and UTC clock dependencies.</summary>
    public LegacyIdentityMappingService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthorizationEvaluator authorizationEvaluator,
        TimeProvider timeProvider)
    {
        _dbFactory = dbFactory;
        _authorizationEvaluator = authorizationEvaluator;
        _timeProvider = timeProvider;
    }

    /// <summary>Registers one exact legacy source key as pending human disposition.</summary>
    /// <param name="reviewer">The authenticated System-scoped reviewer principal.</param>
    /// <param name="sourceTable">The exact legacy table name: Users or Residents.</param>
    /// <param name="sourceKey">The original row key; no normalization or fuzzy matching is performed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new pending map identifier.</returns>
    public async Task<Guid> RegisterPendingAsync(
        ClaimsPrincipal reviewer,
        string sourceTable,
        string sourceKey,
        CancellationToken cancellationToken = default)
    {
        LegacyIdentityMappingValidator.ValidateSource(sourceTable, sourceKey);
        var reviewerId = EnsureReviewerIdentity(reviewer);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await EnsureReviewerAuthorizedAsync(db, reviewer, nowUtc, cancellationToken);

        var exists = await db.LegacyIdentityMaps.AnyAsync(item =>
            item.SourceSystem == SourceSystem && item.SourceTable == sourceTable && item.SourceKey == sourceKey,
            cancellationToken);
        if (exists)
            throw new InvalidOperationException("A mapping candidate already exists for this exact source key.");

        var map = new LegacyIdentityMapEntity
        {
            LegacyIdentityMapId = Guid.NewGuid(),
            SourceSystem = SourceSystem,
            SourceTable = sourceTable,
            SourceKey = sourceKey,
            MappingStatus = (byte)LegacyIdentityMappingStatus.Pending,
            CreatedAtUtc = nowUtc
        };
        db.LegacyIdentityMaps.Add(map);
        db.AuditEvents.Add(CreateAudit(nowUtc, reviewerId, "identity.mapping.candidate.registered", new
        {
            mappingId = map.LegacyIdentityMapId,
            map.SourceTable,
            status = LegacyIdentityMappingStatus.Pending
        }));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return map.LegacyIdentityMapId;
    }

    /// <summary>Applies a human-reviewed identity disposition with transactional target consistency checks.</summary>
    /// <param name="reviewer">The authenticated System-scoped reviewer principal.</param>
    /// <param name="request">The selected disposition, target identifiers, evidence, and reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ReviewAsync(
        ClaimsPrincipal reviewer,
        ReviewLegacyIdentityMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        LegacyIdentityMappingValidator.ValidateReviewRequest(request);
        var reviewerId = EnsureReviewerIdentity(reviewer);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await EnsureReviewerAuthorizedAsync(db, reviewer, nowUtc, cancellationToken);

        var map = await db.LegacyIdentityMaps.SingleOrDefaultAsync(
            item => item.LegacyIdentityMapId == request.MappingId,
            cancellationToken);
        if (map is null)
            throw new InvalidOperationException("The requested mapping candidate does not exist.");
        if (map.SourceSystem != SourceSystem || map.SourceTable is not ("Users" or "Residents"))
            throw new InvalidOperationException("The mapping candidate has an unsupported source table.");
        LegacyIdentityMappingValidator.ValidateSourceSpecificFields(map.SourceTable, request);

        await VerifyTargetsAsync(db, map.SourceTable, request, cancellationToken);

        map.MappingStatus = (byte)request.Status;
        map.PersonId = request.PersonId;
        map.AccountId = request.AccountId;
        map.MatchMethod = NormalizeOptional(request.MatchMethod);
        map.EvidenceReference = NormalizeOptional(request.EvidenceReference);
        map.ReviewReason = request.ReviewReason.Trim();
        map.ReviewedAtUtc = nowUtc;
        map.ReviewedByAccountId = reviewerId;
        map.UpdatedAtUtc = nowUtc;

        db.AuditEvents.Add(CreateAudit(nowUtc, reviewerId, "identity.mapping.reviewed", new
        {
            mappingId = map.LegacyIdentityMapId,
            map.SourceTable,
            request.Status,
            request.PersonId,
            request.AccountId,
            reason = map.ReviewReason
        }));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task EnsureReviewerAuthorizedAsync(
        AppDbContext db,
        ClaimsPrincipal reviewer,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var result = await _authorizationEvaluator.EvaluateAsync(
            db,
            reviewer,
            "identity.mapping.review",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            nowUtc,
            cancellationToken);
        if (!result.Allowed)
            throw new UnauthorizedAccessException("Current authorization is insufficient for identity mapping review.");
    }

    private static async Task VerifyTargetsAsync(
        AppDbContext db,
        string sourceTable,
        ReviewLegacyIdentityMappingRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PersonId is null)
            return;

        var person = await db.Persons.AsNoTracking().SingleOrDefaultAsync(
            item => item.PersonId == request.PersonId.Value,
            cancellationToken);
        if (person is null)
            throw new InvalidOperationException("The selected target person does not exist.");

        if (sourceTable != "Users")
            return;

        var accountPersonId = await db.UserAccounts.AsNoTracking()
            .Where(item => item.AccountId == request.AccountId)
            .Select(item => (Guid?)item.PersonId)
            .SingleOrDefaultAsync(cancellationToken);
        LegacyIdentityMappingValidator.ValidateAccountPersonPair(accountPersonId, request.PersonId.Value);
        if (person.IsArchived)
            throw new InvalidOperationException("An archived person cannot be linked to a legacy user account.");
    }

    private static Guid EnsureReviewerIdentity(ClaimsPrincipal reviewer)
    {
        if (reviewer?.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(reviewer.FindFirstValue(ClaimTypes.NameIdentifier), out var reviewerId))
            throw new UnauthorizedAccessException("An authenticated target account is required.");
        return reviewerId;
    }

    private static AuditEventEntity CreateAudit(DateTime nowUtc, Guid reviewerId, string action, object details) => new()
    {
        AuditEventId = Guid.NewGuid(),
        OccurredAtUtc = nowUtc,
        ActorAccountId = reviewerId,
        ActorReference = reviewerId.ToString("D"),
        EventCategory = "Identity",
        EventType = action,
        ResourceType = "LegacyIdentityMap",
        Outcome = 0,
        SafeDetailsJson = JsonSerializer.Serialize(details)
    };

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Validates exact source identifiers and reviewed identity dispositions.</summary>
internal static class LegacyIdentityMappingValidator
{
    /// <summary>Validates supported source tables and source keys without normalizing the key.</summary>
    internal static void ValidateSource(string sourceTable, string sourceKey)
    {
        if (sourceTable is not ("Users" or "Residents"))
            throw new ArgumentException("Only Users and Residents source rows are supported.", nameof(sourceTable));
        if (string.IsNullOrWhiteSpace(sourceKey) || sourceKey.Length > 256)
            throw new ArgumentException("The exact non-empty source key, up to 256 characters, is required.", nameof(sourceKey));
    }

    /// <summary>Validates disposition-specific targets and required review evidence.</summary>
    internal static void ValidateReviewRequest(ReviewLegacyIdentityMappingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MappingId == Guid.Empty || !Enum.IsDefined(request.Status) || request.Status == LegacyIdentityMappingStatus.Pending)
            throw new ArgumentException("A mapping identifier and final reviewed disposition are required.", nameof(request));

        var resolved = request.Status is LegacyIdentityMappingStatus.Linked or LegacyIdentityMappingStatus.Separate;
        if (resolved)
        {
            if (request.PersonId is null || request.PersonId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.MatchMethod) || request.MatchMethod.Trim().Length > 80 ||
                string.IsNullOrWhiteSpace(request.EvidenceReference) || request.EvidenceReference.Trim().Length > 500)
                throw new ArgumentException("Linked or separate dispositions require a person, match method, and evidence reference.", nameof(request));
        }
        else if (request.PersonId is not null || request.AccountId is not null)
        {
            throw new ArgumentException("Rejected and manual-review dispositions cannot have target identities.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ReviewReason) || request.ReviewReason.Trim().Length > 1000)
            throw new ArgumentException("A review reason between 1 and 1000 characters is required.", nameof(request));
    }

    /// <summary>Validates the User versus Resident target shape for a reviewed mapping.</summary>
    internal static void ValidateSourceSpecificFields(string sourceTable, ReviewLegacyIdentityMappingRequest request)
    {
        if (sourceTable is not ("Users" or "Residents"))
            throw new ArgumentException("Only Users and Residents source rows are supported.", nameof(sourceTable));

        var resolved = request.Status is LegacyIdentityMappingStatus.Linked or LegacyIdentityMappingStatus.Separate;
        if (!resolved)
            return;

        if (sourceTable == "Users" && request.AccountId is null)
            throw new ArgumentException("A Users source row must resolve to both Person and UserAccount.", nameof(request));
        if (sourceTable == "Residents" && request.AccountId is not null)
            throw new ArgumentException("A Residents source row resolves to Person only; it does not create a login account.", nameof(request));
        if (request.AccountId == Guid.Empty)
            throw new ArgumentException("A supplied account identifier cannot be empty.", nameof(request));
    }

    /// <summary>Enforces that a UserAccount and Person selected for migration are the same target identity.</summary>
    internal static void ValidateAccountPersonPair(Guid? accountPersonId, Guid selectedPersonId)
    {
        if (accountPersonId is null || accountPersonId.Value != selectedPersonId)
            throw new InvalidOperationException("The selected account and person do not belong to the same target identity.");
    }
}
