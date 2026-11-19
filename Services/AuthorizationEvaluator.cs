using System.Globalization;
using System.Security.Claims;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Final outcome of one target-model authorization evaluation.</summary>
public enum AuthorizationDecision
{
    Deny = 0,
    Allow = 1,
    RequiresStepUp = 2
}

/// <summary>Stable, non-sensitive reason for an authorization outcome.</summary>
public enum AuthorizationReason
{
    Granted,
    NotAuthenticated,
    InvalidActor,
    UnknownPermission,
    InvalidResource,
    AccountInactive,
    StaleSecurityStamp,
    StepUpRequired,
    MfaNotEnrolled,
    ExplicitDeny,
    NoApplicableAllow
}

/// <summary>Server-resolved resource scope supplied by a protected use case.</summary>
public sealed record ResolvedAuthorizationResource(
    AuthorizationScopeType ScopeType,
    string? BuildingId = null,
    string? ApartmentId = null,
    Guid? PersonId = null);

/// <summary>Authorization outcome without exposing resource or identity details.</summary>
public sealed record AuthorizationEvaluationResult(
    AuthorizationDecision Decision,
    AuthorizationReason Reason)
{
    /// <summary>Whether the operation was allowed.</summary>
    public bool Allowed => Decision == AuthorizationDecision.Allow;
}

/// <summary>
/// Evaluates target-model permission grants. Callers must pass a resource descriptor
/// resolved from server-side data; client-provided scope identifiers are not trusted.
/// </summary>
public sealed class AuthorizationEvaluator
{
    private const string SecurityStampClaim = "securityStamp";
    private const string MfaVerifiedAtClaim = "mfaVerifiedAtUtc";

    private readonly AppDbContext _db;
    private readonly IAuthorizationPermissionCatalog _catalog;

    /// <summary>Creates the evaluator for the current dependency-injection scope.</summary>
    public AuthorizationEvaluator(AppDbContext db, IAuthorizationPermissionCatalog catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    /// <summary>Evaluates an authenticated principal against current account, role, and override data.</summary>
    /// <param name="principal">The server-authenticated principal.</param>
    /// <param name="permissionKey">The stable permission key requested by the use case.</param>
    /// <param name="resource">A resource scope resolved by server-side application code.</param>
    /// <param name="nowUtc">Current UTC time from the caller's clock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A generic decision and safe reason code.</returns>
    public Task<AuthorizationEvaluationResult> EvaluateAsync(
        ClaimsPrincipal principal,
        string permissionKey,
        ResolvedAuthorizationResource? resource,
        DateTime nowUtc,
        CancellationToken cancellationToken = default) =>
        EvaluateAsync(_db, principal, permissionKey, resource, nowUtc, cancellationToken);

    internal async Task<AuthorizationEvaluationResult> EvaluateAsync(
        AppDbContext db,
        ClaimsPrincipal principal,
        string permissionKey,
        ResolvedAuthorizationResource? resource,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return Deny(AuthorizationReason.NotAuthenticated);

        if (nowUtc.Kind != DateTimeKind.Utc ||
            !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId))
            return Deny(AuthorizationReason.InvalidActor);

        if (!_catalog.TryGet(permissionKey, out var definition))
            return Deny(AuthorizationReason.UnknownPermission);

        if (!IsValidResourceShape(resource) || !definition.AllowedGrantScopes.Any())
            return Deny(AuthorizationReason.InvalidResource);

        var account = await (
            from userAccount in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on userAccount.PersonId equals person.PersonId
            where userAccount.AccountId == accountId
            select new
            {
                userAccount.AccountStatus,
                userAccount.PersonId,
                userAccount.SecurityStamp,
                person.IsArchived
            }).SingleOrDefaultAsync(cancellationToken);

        if (account is null || account.AccountStatus != 1 || account.IsArchived)
            return Deny(AuthorizationReason.AccountInactive);

        var sessionStamp = principal.FindFirstValue(SecurityStampClaim);
        if (string.IsNullOrWhiteSpace(sessionStamp) ||
            !string.Equals(sessionStamp, account.SecurityStamp, StringComparison.Ordinal))
            return Deny(AuthorizationReason.StaleSecurityStamp);

        if (!await ResourceExistsAsync(db, resource!, account.PersonId, cancellationToken))
            return Deny(AuthorizationReason.InvalidResource);

        var roleGrants = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
            join rolePermission in db.RolePermissions.AsNoTracking() on role.RoleKey equals rolePermission.RoleKey
            where assignment.AccountId == accountId
                  && role.IsActive
                  && rolePermission.PermissionKey == definition.Key
                  && assignment.ValidFromUtc <= nowUtc
                  && (assignment.ValidToUtc == null || assignment.ValidToUtc > nowUtc)
                  && assignment.RevokedAtUtc == null
            select new GrantCandidate(assignment.ScopeType, assignment.ScopeBuildingId, assignment.ScopeApartmentId, rolePermission.Effect))
            .ToListAsync(cancellationToken);

        var overrides = await db.AccountPermissionOverrides.AsNoTracking()
            .Where(item => item.AccountId == accountId
                           && item.PermissionKey == definition.Key
                           && item.ValidFromUtc <= nowUtc
                           && (item.ValidToUtc == null || item.ValidToUtc > nowUtc)
                           && item.RevokedAtUtc == null)
            .Select(item => new GrantCandidate(item.ScopeType, item.ScopeBuildingId, item.ScopeApartmentId, item.Effect))
            .ToListAsync(cancellationToken);

        var applicableEffects = roleGrants.Concat(overrides)
            .Where(candidate => IsApplicable(candidate, definition, resource!, account.PersonId))
            .Select(candidate => candidate.Effect)
            .ToArray();

        // Database CHECK constraints reject unknown effect values. Treat any unexpected
        // value as a deny in case data was imported outside the approved migration path.
        if (applicableEffects.Any(effect => effect is not (0 or 1)) ||
            applicableEffects.Contains((byte)AuthorizationEffect.Deny))
            return Deny(AuthorizationReason.ExplicitDeny);

        if (applicableEffects.Contains((byte)AuthorizationEffect.Allow) && definition.RequiresFreshMfa)
        {
            var mfaEnabled = await db.UserTwoFactorSettings.AsNoTracking()
                .AnyAsync(item => item.AccountId == accountId && item.IsEnabled, cancellationToken);
            if (!mfaEnabled)
                return Deny(AuthorizationReason.MfaNotEnrolled);

            if (!HasFreshMfa(principal, definition, nowUtc))
                return new AuthorizationEvaluationResult(AuthorizationDecision.RequiresStepUp, AuthorizationReason.StepUpRequired);
        }

        if (applicableEffects.Contains((byte)AuthorizationEffect.Allow))
            return new AuthorizationEvaluationResult(AuthorizationDecision.Allow, AuthorizationReason.Granted);

        return Deny(AuthorizationReason.NoApplicableAllow);
    }

    private async Task<bool> ResourceExistsAsync(
        AppDbContext db,
        ResolvedAuthorizationResource resource,
        Guid actorPersonId,
        CancellationToken cancellationToken)
    {
        return resource.ScopeType switch
        {
            AuthorizationScopeType.System => true,
            AuthorizationScopeType.Building => await db.Buildings.AsNoTracking()
                .AnyAsync(item => item.Id == resource.BuildingId, cancellationToken),
            AuthorizationScopeType.Apartment => await db.Apartments.AsNoTracking()
                .AnyAsync(item => item.BuildingId == resource.BuildingId && item.Id == resource.ApartmentId, cancellationToken),
            AuthorizationScopeType.Personal => resource.PersonId == actorPersonId,
            _ => false
        };
    }

    private static bool IsValidResourceShape(ResolvedAuthorizationResource? resource)
    {
        if (resource is null || !Enum.IsDefined(resource.ScopeType))
            return false;

        return resource.ScopeType switch
        {
            AuthorizationScopeType.System => resource.BuildingId is null && resource.ApartmentId is null && resource.PersonId is null,
            AuthorizationScopeType.Building => !string.IsNullOrWhiteSpace(resource.BuildingId) && resource.ApartmentId is null && resource.PersonId is null,
            AuthorizationScopeType.Apartment => !string.IsNullOrWhiteSpace(resource.BuildingId) && !string.IsNullOrWhiteSpace(resource.ApartmentId) && resource.PersonId is null,
            AuthorizationScopeType.Personal => resource.BuildingId is null && resource.ApartmentId is null && resource.PersonId is not null,
            _ => false
        };
    }

    private static bool IsApplicable(
        GrantCandidate candidate,
        AuthorizationPermissionDefinition definition,
        ResolvedAuthorizationResource resource,
        Guid actorPersonId)
    {
        if (!Enum.IsDefined((AuthorizationScopeType)candidate.ScopeType))
            return false;

        var grantScope = (AuthorizationScopeType)candidate.ScopeType;
        if (!definition.AllowedGrantScopes.Contains(grantScope))
            return false;

        return grantScope switch
        {
            AuthorizationScopeType.System => resource.ScopeType == AuthorizationScopeType.System ||
                (definition.SystemGrantIncludesChildResources && resource.ScopeType is AuthorizationScopeType.Building or AuthorizationScopeType.Apartment),
            AuthorizationScopeType.Building =>
                !string.IsNullOrWhiteSpace(candidate.ScopeBuildingId) &&
                string.Equals(candidate.ScopeBuildingId, resource.BuildingId, StringComparison.Ordinal) &&
                candidate.ScopeApartmentId is null &&
                resource.ScopeType is AuthorizationScopeType.Building or AuthorizationScopeType.Apartment,
            AuthorizationScopeType.Apartment =>
                resource.ScopeType == AuthorizationScopeType.Apartment &&
                string.Equals(candidate.ScopeBuildingId, resource.BuildingId, StringComparison.Ordinal) &&
                string.Equals(candidate.ScopeApartmentId, resource.ApartmentId, StringComparison.Ordinal),
            AuthorizationScopeType.Personal => resource.ScopeType == AuthorizationScopeType.Personal && resource.PersonId == actorPersonId,
            _ => false
        };
    }

    private static bool HasFreshMfa(
        ClaimsPrincipal principal,
        AuthorizationPermissionDefinition definition,
        DateTime nowUtc)
    {
        var raw = principal.FindFirstValue(MfaVerifiedAtClaim);
        if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var verifiedAt) ||
            verifiedAt.Kind != DateTimeKind.Utc ||
            verifiedAt > nowUtc)
            return false;

        var window = definition.MfaFreshnessWindow ?? TimeSpan.FromMinutes(10);
        return nowUtc - verifiedAt <= window;
    }

    private static AuthorizationEvaluationResult Deny(AuthorizationReason reason) =>
        new(AuthorizationDecision.Deny, reason);

    private sealed record GrantCandidate(byte ScopeType, string? ScopeBuildingId, string? ScopeApartmentId, byte Effect);
}
