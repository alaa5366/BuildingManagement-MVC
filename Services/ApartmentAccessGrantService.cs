using System.Data;
using System.Security.Claims;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Describes a time-bounded grant of an apartment access profile.</summary>
public sealed record GrantApartmentAccessRequest(
    string BuildingId,
    string ApartmentId,
    Guid PersonId,
    ApartmentAccessGrantType GrantType,
    DateTime ValidFromUtc,
    DateTime? ValidToUtc,
    string Reason,
    Guid GrantedByAccountId);

/// <summary>Describes revocation of an existing apartment access grant.</summary>
public sealed record RevokeApartmentAccessRequest(
    Guid GrantId,
    Guid RevokedByAccountId,
    string Reason);

/// <summary>Creates and revokes apartment access grants without assigning implicit permissions.</summary>
public sealed class ApartmentAccessGrantService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly AuthorizationEvaluator _authorizationEvaluator;
    private readonly TimeProvider _timeProvider;

    /// <summary>Creates the service with the configured database factory and UTC clock.</summary>
    public ApartmentAccessGrantService(
        IDbContextFactory<AppDbContext> dbFactory,
        AuthorizationEvaluator authorizationEvaluator,
        TimeProvider timeProvider)
    {
        _dbFactory = dbFactory;
        _authorizationEvaluator = authorizationEvaluator;
        _timeProvider = timeProvider;
    }

    /// <summary>Creates a person-to-apartment grant with a code-owned profile key.</summary>
    /// <param name="actor">The authenticated principal used for current permission and MFA evaluation.</param>
    /// <param name="request">The apartment, person, profile, effective period, reason, and actor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new grant identifier.</returns>
    public async Task<Guid> GrantAsync(
        ClaimsPrincipal actor,
        GrantApartmentAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        ApartmentAccessGrantValidator.ValidateGrantRequest(request);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var actorAccountId = EnsureActorIdentity(actor, request.GrantedByAccountId);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await EnsureAuthorizedActorAsync(
            db,
            actor,
            actorAccountId,
            request.BuildingId,
            request.ApartmentId,
            nowUtc,
            cancellationToken);

        var apartmentExists = await db.Apartments.AsNoTracking().AnyAsync(
            item => item.BuildingId == request.BuildingId && item.Id == request.ApartmentId,
            cancellationToken);
        if (!apartmentExists)
            throw new InvalidOperationException("The requested apartment does not exist.");

        var personExists = await db.Persons.AsNoTracking().AnyAsync(
            item => item.PersonId == request.PersonId && !item.IsArchived,
            cancellationToken);
        if (!personExists)
            throw new InvalidOperationException("The requested person does not exist or is archived.");

        await EnsureActiveAccountAsync(db, request.GrantedByAccountId, cancellationToken);

        var grant = new ApartmentAccessGrantEntity
        {
            GrantId = Guid.NewGuid(),
            BuildingId = request.BuildingId,
            ApartmentId = request.ApartmentId,
            PersonId = request.PersonId,
            GrantType = request.GrantType,
            PermissionSetKey = ApartmentAccessPermissionProfileCatalog.For(request.GrantType),
            ValidFromUtc = request.ValidFromUtc,
            ValidToUtc = request.ValidToUtc,
            GrantedAtUtc = nowUtc,
            GrantedByAccountId = request.GrantedByAccountId,
            Reason = request.Reason.Trim()
        };

        db.ApartmentAccessGrants.Add(grant);
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            OccurredAtUtc = nowUtc,
            ActorAccountId = request.GrantedByAccountId,
            ActorReference = request.GrantedByAccountId.ToString("D"),
            BuildingId = request.BuildingId,
            EventCategory = "ApartmentAccess",
            EventType = "apartment.access.granted",
            ResourceType = "ApartmentAccessGrant",
            ResourceReference = grant.GrantId.ToString("D"),
            Outcome = 0,
            SafeDetailsJson = JsonSerializer.Serialize(new
            {
                grantId = grant.GrantId,
                request.PersonId,
                request.GrantType,
                grant.PermissionSetKey,
                request.ValidFromUtc,
                request.ValidToUtc,
                reason = grant.Reason
            })
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return grant.GrantId;
    }

    /// <summary>Revokes an unrevoked grant and appends its reason to the audit log atomically.</summary>
    /// <param name="actor">The authenticated principal used for current permission and MFA evaluation.</param>
    /// <param name="request">The grant, active revoking account, and non-empty reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RevokeAsync(
        ClaimsPrincipal actor,
        RevokeApartmentAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        ApartmentAccessGrantValidator.ValidateRevokeRequest(request);
        EnsureActorIdentity(actor, request.RevokedByAccountId);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await EnsureActiveAccountAsync(db, request.RevokedByAccountId, cancellationToken);

        var grant = await db.ApartmentAccessGrants.SingleOrDefaultAsync(
            item => item.GrantId == request.GrantId,
            cancellationToken);
        if (grant is null)
            throw new InvalidOperationException("The requested apartment access grant does not exist.");
        if (grant.RevokedAtUtc is not null)
            throw new InvalidOperationException("The apartment access grant has already been revoked.");

        var actorAccountId = await EnsureAuthorizedActorAsync(
            db,
            actor,
            request.RevokedByAccountId,
            grant.BuildingId,
            grant.ApartmentId,
            nowUtc,
            cancellationToken);

        grant.RevokedAtUtc = nowUtc;
        grant.RevokedByAccountId = actorAccountId;
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(),
            OccurredAtUtc = nowUtc,
            ActorAccountId = request.RevokedByAccountId,
            ActorReference = request.RevokedByAccountId.ToString("D"),
            BuildingId = grant.BuildingId,
            EventCategory = "ApartmentAccess",
            EventType = "apartment.access.revoked",
            ResourceType = "ApartmentAccessGrant",
            ResourceReference = grant.GrantId.ToString("D"),
            Outcome = 0,
            SafeDetailsJson = JsonSerializer.Serialize(new
            {
                grantId = grant.GrantId,
                grant.PersonId,
                reason = request.Reason.Trim()
            })
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task EnsureActiveAccountAsync(
        AppDbContext db,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var active = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == accountId
                  && account.AccountStatus == 1
                  && !person.IsArchived
            select account.AccountId).AnyAsync(cancellationToken);

        if (!active)
            throw new InvalidOperationException("The granting account must be active.");
    }

    private async Task<Guid> EnsureAuthorizedActorAsync(
        AppDbContext db,
        ClaimsPrincipal actor,
        Guid expectedAccountId,
        string buildingId,
        string apartmentId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var actorAccountId = EnsureActorIdentity(actor, expectedAccountId);

        var result = await _authorizationEvaluator.EvaluateAsync(
            db,
            actor,
            "apartment.access.grant.manage",
            new ResolvedAuthorizationResource(AuthorizationScopeType.Apartment, buildingId, apartmentId),
            nowUtc,
            cancellationToken);
        if (!result.Allowed)
            throw new UnauthorizedAccessException("Current authorization is insufficient for this apartment access operation.");

        return actorAccountId;
    }

    private static Guid EnsureActorIdentity(ClaimsPrincipal actor, Guid expectedAccountId)
    {
        if (actor?.Identity?.IsAuthenticated != true ||
            !Guid.TryParse(actor.FindFirstValue(ClaimTypes.NameIdentifier), out var actorAccountId) ||
            actorAccountId != expectedAccountId)
            throw new UnauthorizedAccessException("An authenticated matching account is required.");

        return actorAccountId;
    }

}

/// <summary>Validates apartment access grant inputs before database operations.</summary>
internal static class ApartmentAccessGrantValidator
{
    /// <summary>Checks grant fields and profile mapping.</summary>
    internal static void ValidateGrantRequest(GrantApartmentAccessRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.BuildingId) || request.BuildingId.Length > 64 ||
            string.IsNullOrWhiteSpace(request.ApartmentId) || request.ApartmentId.Length > 64 ||
            request.PersonId == Guid.Empty || request.GrantedByAccountId == Guid.Empty)
            throw new ArgumentException("Building, apartment, person, and actor identifiers are required.", nameof(request));
        if (!Enum.IsDefined(request.GrantType))
            throw new ArgumentException("The apartment access grant type is unknown.", nameof(request));
        if (request.ValidFromUtc.Kind != DateTimeKind.Utc ||
            (request.ValidToUtc is not null && request.ValidToUtc.Value.Kind != DateTimeKind.Utc) ||
            (request.ValidToUtc is not null && request.ValidToUtc.Value <= request.ValidFromUtc))
            throw new ArgumentException("Grant dates must be UTC and the end must be after the start.", nameof(request));
        if ((request.GrantType is ApartmentAccessGrantType.Emergency or ApartmentAccessGrantType.Temporary) && request.ValidToUtc is null)
            throw new ArgumentException("Emergency and temporary grants require an expiry time.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            throw new ArgumentException("A grant reason between 1 and 500 characters is required.", nameof(request));

        _ = ApartmentAccessPermissionProfileCatalog.For(request.GrantType);
    }

    /// <summary>Checks the revocation identifier, actor, and reason.</summary>
    internal static void ValidateRevokeRequest(RevokeApartmentAccessRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.GrantId == Guid.Empty || request.RevokedByAccountId == Guid.Empty)
            throw new ArgumentException("Grant and revoking account identifiers are required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            throw new ArgumentException("A revocation reason between 1 and 500 characters is required.", nameof(request));
    }
}
