using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Re-checks signed-in users against the database (disabled, removed, permissions changed) on every cookie validation, with a short cache.
/// </summary>
public class SessionRevalidator
{
    private static readonly TimeSpan ImpersonationCacheFor = TimeSpan.FromSeconds(30);

    private readonly ConcurrentDictionary<string, long> _permissionVersions = new(StringComparer.Ordinal);

    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SessionRevalidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionRevalidator"/> class.
    /// </summary>
    public SessionRevalidator(
        IDbContextFactory<AppDbContext> sqlFactory,
        IMemoryCache cache,
        ILogger<SessionRevalidator> logger)
    {
        _sqlFactory = sqlFactory;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Outcome of a session validation: whether the session is still valid and the refreshed permissions of the user.
    /// </summary>
    public sealed record Result(bool Valid, List<string>? Permissions);

    /// <summary>
    /// Invalidates cached authorization results for an administrator immediately after an account or permission change.
    /// </summary>
    public void InvalidateAdmin(string uid)
    {
        if (!string.IsNullOrWhiteSpace(uid))
            _permissionVersions.AddOrUpdate(uid, 1, static (_, version) => checked(version + 1));
    }

    /// <summary>
    /// Validates a signed-in principal against the database. Returns the refreshed permissions for admins, or rejects the session when the account was disabled or removed.
    /// </summary>
    public async Task<Result> ValidateAsync(ClaimsPrincipal user)
    {
        // Impersonated principals are validated against the initiating target-schema account.
        if (user.HasClaim("impersonated", "true"))
            return await ValidateImpersonatedAsync(user);

        // Legacy cookies are rejected without querying retired Users/Residents tables.
        // All newly issued sessions must carry the canonical target identity marker.
        if (!user.HasClaim("targetIdentityAccount", "true"))
            return new Result(false, null);

        return await ValidateTargetIdentityAccountAsync(user);
    }

    private async Task<Result> ValidateTargetIdentitySuperAdminAsync(ClaimsPrincipal user, Guid accountId)
    {
        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email) || !SuperAdminIdentityPolicy.IsEmailAllowed(email))
            return new Result(false, null);

        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            var account = await (
                from item in db.UserAccounts.AsNoTracking()
                join person in db.Persons.AsNoTracking() on item.PersonId equals person.PersonId
                where item.AccountId == accountId
                select new { item.AccountStatus, item.SecurityStamp, item.NormalizedEmail, person.IsArchived })
                .SingleOrDefaultAsync();
            if (account is null || account.AccountStatus != 1 || account.IsArchived ||
                !string.Equals(account.NormalizedEmail, email.Trim().ToUpperInvariant(), StringComparison.Ordinal) ||
                !string.Equals(account.SecurityStamp, user.FindFirst("securityStamp")?.Value, StringComparison.Ordinal))
                return new Result(false, null);

            var activeAssignment = await (
                from assignment in db.UserRoleAssignments.AsNoTracking()
                join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
                where assignment.AccountId == accountId && assignment.RoleKey == "superadmin"
                    && role.IsActive && assignment.ScopeType == 0
                    && assignment.ScopeBuildingId == null && assignment.ScopeApartmentId == null
                    && assignment.ValidFromUtc <= now
                    && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                    && assignment.RevokedAtUtc == null
                select assignment.RoleAssignmentId).AnyAsync();
            if (!activeAssignment) return new Result(false, null);

            var twoFactorEnabled = await db.UserTwoFactorSettings.AsNoTracking()
                .AnyAsync(item => item.AccountId == accountId && item.IsEnabled);
            if (!twoFactorEnabled || !user.HasClaim("twoFactorVerified", "true"))
                return new Result(false, null);

            var permissions = await db.RolePermissions.AsNoTracking()
                .Where(item => item.RoleKey == "superadmin" && item.Effect == 1)
                .Select(item => item.PermissionKey).Distinct().ToListAsync();
            return new Result(true, permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SessionRevalidator] target SuperAdmin validation failed for {AccountId}; rejecting session", accountId);
            return new Result(false, null);
        }
    }

    private async Task<Result> ValidateImpersonatedAsync(ClaimsPrincipal user)
    {
        var actorUid = user.FindFirst("impersonationActorUid")?.Value;
        var actorRole = user.FindFirst("impersonationActorRole")?.Value;
        var actorEmail = user.FindFirst("impersonationActorEmail")?.Value;
        var actorPermissionsJson = user.FindFirst("impersonationActorPermissions")?.Value;
        var actorTwoFactorVerified = user.FindFirst("impersonationActorTwoFactorVerified")?.Value;
        var actorSecurityStamp = user.FindFirst("impersonationActorSecurityStamp")?.Value;
        var buildingId = user.FindFirst("impersonationTargetBuildingId")?.Value;
        var apartmentId = user.FindFirst("impersonationTargetApartmentId")?.Value;
        var effectiveRole = user.FindFirst(ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(actorUid, out var actorAccountId) ||
            !string.Equals(user.FindFirst("impersonatedBy")?.Value, actorUid, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(buildingId) ||
            (actorRole != "admin" && actorRole != "superadmin") ||
            (actorRole == "admin" && string.IsNullOrWhiteSpace(actorPermissionsJson)) ||
            (actorRole == "superadmin" && string.IsNullOrWhiteSpace(actorEmail)) ||
            (effectiveRole == "resident" && string.IsNullOrWhiteSpace(apartmentId)) ||
            (effectiveRole == "admin" && !string.IsNullOrWhiteSpace(apartmentId)) ||
            (effectiveRole != "resident" && effectiveRole != "admin"))
            return new Result(false, null);

        var actorPermissionVersion = _permissionVersions.GetOrAdd(actorUid!, 0);
        var cacheKey = $"sessval:imp:{actorUid}:v:{actorPermissionVersion}:{actorRole}:{buildingId}:{apartmentId ?? "-"}:{actorSecurityStamp}:{actorTwoFactorVerified}";
        if (_cache.TryGetValue(cacheKey, out Result? cached) && cached != null)
            return cached;

        try
        {
            var actorClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, actorUid!),
                new(ClaimTypes.Role, actorRole!),
                new("securityStamp", actorSecurityStamp ?? ""),
                new("targetIdentityAccount", "true"),
                new("twoFactorVerified", actorTwoFactorVerified ?? "false"),
                new("buildingId", buildingId)
            };
            if (!string.IsNullOrWhiteSpace(actorEmail))
                actorClaims.Add(new Claim(ClaimTypes.Email, actorEmail));
            var actorPrincipal = new ClaimsPrincipal(new ClaimsIdentity(actorClaims, "ImpersonationValidation"));

            var actorResult = actorRole == "superadmin"
                ? await ValidateTargetIdentitySuperAdminAsync(actorPrincipal, actorAccountId)
                : await ValidateTargetIdentityAdminAsync(actorPrincipal, actorAccountId);
            if (!actorResult.Valid)
            {
                var rejected = new Result(false, null);
                _cache.Set(cacheKey, rejected, ImpersonationCacheFor);
                return rejected;
            }

            if (actorRole == "admin")
            {
                var capturedPermissions = JsonSerializer.Deserialize<List<string>>(actorPermissionsJson!) ?? new List<string>();
                if (!capturedPermissions.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)
                    .SequenceEqual((actorResult.Permissions ?? new List<string>()).Distinct(StringComparer.Ordinal)
                        .OrderBy(x => x, StringComparer.Ordinal), StringComparer.Ordinal))
                {
                    var rejected = new Result(false, null);
                    _cache.Set(cacheKey, rejected, ImpersonationCacheFor);
                    return rejected;
                }
            }

            await using var db = await _sqlFactory.CreateDbContextAsync();
            var buildingExists = await db.Buildings.AsNoTracking().AnyAsync(item => item.Id == buildingId);
            var apartmentExists = string.IsNullOrWhiteSpace(apartmentId) ||
                await db.Apartments.AsNoTracking().AnyAsync(item => item.BuildingId == buildingId && item.Id == apartmentId);
            var result = new Result(buildingExists && apartmentExists, null);
            _cache.Set(cacheKey, result, ImpersonationCacheFor);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SessionRevalidator] Target identity impersonation validation failed for actor {ActorUid}", actorUid);
            return new Result(false, null);
        }
    }

    private async Task<Result> ValidateTargetIdentityAdminAsync(ClaimsPrincipal user, Guid accountId)
    {
        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            var account = await (
                from item in db.UserAccounts.AsNoTracking()
                join person in db.Persons.AsNoTracking() on item.PersonId equals person.PersonId
                where item.AccountId == accountId
                select new { item.AccountStatus, item.SecurityStamp, person.IsArchived })
                .SingleOrDefaultAsync();
            if (account is null || account.AccountStatus != 1 || account.IsArchived ||
                !string.Equals(account.SecurityStamp, user.FindFirst("securityStamp")?.Value, StringComparison.Ordinal))
                return new Result(false, null);

            var claimedBuildings = user.FindAll("buildingId").Select(item => item.Value)
                .ToHashSet(StringComparer.Ordinal);
            if (claimedBuildings.Count == 0) return new Result(false, null);

            var activeBuildings = await db.UserRoleAssignments.AsNoTracking()
                .Where(item => item.AccountId == accountId && (item.RoleKey == "admin" || item.RoleKey == "building-admin")
                    && item.ScopeType == 1 && item.ScopeBuildingId != null
                    && item.ScopeApartmentId == null && item.RevokedAtUtc == null
                    && item.ValidFromUtc <= now
                    && (item.ValidToUtc == null || item.ValidToUtc > now))
                .Join(db.RoleDefinitions.AsNoTracking().Where(role => (role.RoleKey == "admin" || role.RoleKey == "building-admin") && role.IsActive),
                    assignment => assignment.RoleKey, role => role.RoleKey,
                    (assignment, role) => assignment.ScopeBuildingId!)
                .Distinct().ToListAsync();
            if (activeBuildings.Count == 0 || claimedBuildings.Any(building => !activeBuildings.Contains(building, StringComparer.Ordinal)))
                return new Result(false, null);

            var twoFactorEnabled = await db.UserTwoFactorSettings.AsNoTracking()
                .AnyAsync(item => item.AccountId == accountId && item.IsEnabled);
            if (!twoFactorEnabled || !user.HasClaim("twoFactorVerified", "true"))
                return new Result(false, null);

            var basePermissions = await db.RolePermissions.AsNoTracking()
                .Where(item => (item.RoleKey == "admin" || item.RoleKey == "building-admin") && item.Effect == 1)
                .Select(item => item.PermissionKey).Distinct().ToListAsync();
            var overrides = await db.AccountPermissionOverrides.AsNoTracking()
                .Where(item => item.AccountId == accountId && item.ScopeType == 1
                    && item.ScopeBuildingId != null && claimedBuildings.Contains(item.ScopeBuildingId)
                    && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)
                    && item.RevokedAtUtc == null)
                .ToListAsync();
            var perBuilding = claimedBuildings.Select(building =>
            {
                var scoped = overrides.Where(item => item.ScopeBuildingId == building).ToList();
                return basePermissions.Where(permission => !scoped.Any(item =>
                        item.PermissionKey == permission && item.Effect == 0))
                    .Concat(scoped.Where(item => item.Effect == 1).Select(item => item.PermissionKey))
                    .Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
            }).ToList();
            var permissions = perBuilding.Skip(1).Aggregate(perBuilding[0], (current, next) =>
            {
                current.IntersectWith(next);
                return current;
            }).OrderBy(value => value, StringComparer.Ordinal).ToList();
            return new Result(true, permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SessionRevalidator] target admin validation failed for {AccountId}; rejecting session", accountId);
            return new Result(false, null);
        }
    }

    private async Task<Result> ValidateTargetIdentityAccountAsync(ClaimsPrincipal user)
    {
        var claimedRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var accountId))
            return new Result(false, null);
        if (claimedRole == "superadmin")
            return await ValidateTargetIdentitySuperAdminAsync(user, accountId);
        if (claimedRole == "admin")
            return await ValidateTargetIdentityAdminAsync(user, accountId);
        if (claimedRole != "resident")
            return new Result(false, null);

        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var now = DateTime.UtcNow;
            var buildingId = user.FindFirst("buildingId")?.Value;
            var apartmentId = user.FindFirst("apartmentId")?.Value;
            var account = await (
                from item in db.UserAccounts.AsNoTracking()
                join person in db.Persons.AsNoTracking() on item.PersonId equals person.PersonId
                where item.AccountId == accountId
                select new { item.AccountStatus, item.SecurityStamp, person.PersonId, person.IsArchived })
                .SingleOrDefaultAsync();
            if (account is null || account.AccountStatus != 1 || account.IsArchived ||
                !string.Equals(account.SecurityStamp, user.FindFirst("securityStamp")?.Value, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(apartmentId))
                return new Result(false, null);

            var hasSuperAdminAssignment = await db.UserRoleAssignments.AsNoTracking()
                .AnyAsync(assignment => assignment.AccountId == accountId
                    && assignment.RoleKey == "superadmin" && assignment.RevokedAtUtc == null);
            if (hasSuperAdminAssignment) return new Result(false, null);

            var activeResidentRole = await (
                from assignment in db.UserRoleAssignments.AsNoTracking()
                join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
                where assignment.AccountId == accountId && assignment.RoleKey == "resident" && role.IsActive
                    && assignment.RevokedAtUtc == null && assignment.ValidFromUtc <= now
                    && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                    && (
                        (assignment.ScopeType == 1 && assignment.ScopeBuildingId == buildingId
                            && assignment.ScopeApartmentId == null) ||
                        (assignment.ScopeType == 2 && assignment.ScopeBuildingId == buildingId
                            && assignment.ScopeApartmentId == apartmentId))
                select assignment.RoleAssignmentId).AnyAsync();
            if (!activeResidentRole) return new Result(false, null);

            var activeOccupancy = await db.ApartmentOccupancies.AsNoTracking().AnyAsync(item =>
                item.PersonId == account.PersonId && item.BuildingId == buildingId && item.ApartmentId == apartmentId
                && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now));
            return new Result(activeOccupancy, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SessionRevalidator] target identity account validation failed for {AccountId}; rejecting session", accountId);
            return new Result(false, null);
        }
    }
}
