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
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ImpersonationCacheFor = TimeSpan.FromSeconds(30);

    // لو الـ DB وقعت لحظيًا: نكمّل بآخر نتيجة سليمة لحد 15 دقيقة، وبعدها نقفل الجلسة (fail closed)
    private static readonly TimeSpan LastKnownGoodFor = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, long> _permissionVersions = new(StringComparer.Ordinal);

    private readonly UsersService _users;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SessionRevalidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionRevalidator"/> class.
    /// </summary>
    public SessionRevalidator(
        UsersService users,
        IDbContextFactory<AppDbContext> sqlFactory,
        IMemoryCache cache,
        ILogger<SessionRevalidator> logger)
    {
        _users = users;
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
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        // Impersonated principals are validated against the initiating account and target scope.
        // Synthetic resident identities must never be treated as ordinary resident database users.
        if (user.HasClaim("impersonated", "true"))
            return await ValidateImpersonatedAsync(user);

        if (user.HasClaim("targetIdentityAccount", "true"))
            return await ValidateTargetIdentityAccountAsync(user);

        // The authenticated role claim is authoritative for normal sign-ins.
        // An allow-listed e-mail must not turn a resident/admin context into a super-admin
        // session; the allow-list is checked inside ValidateSuperAdminPrincipalAsync.
        if (IsSuperAdminPrincipal(user))
            return await ValidateSuperAdminPrincipalAsync(user);

        // ✅ الساكن (دخول عادي أو QR): لازم لسه موجود ونشط وغير معطّل
        if (role == "resident")
            return await ValidateResidentAsync(user);

        if (role != "admin") return new Result(false, null);

        var uid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return new Result(false, null);

        var claimedBuildings = user.FindAll("buildingId").Select(c => c.Value).OrderBy(x => x).ToList();
        var twoFactorClaim = user.FindFirst("twoFactorVerified")?.Value ?? "missing";
        var permissionVersion = _permissionVersions.GetOrAdd(uid, 0);
        var cacheKey = "sessval:" + uid + ":v:" + permissionVersion + ":" + string.Join(",", claimedBuildings) + ":2fa:" + twoFactorClaim;
        if (_cache.TryGetValue(cacheKey, out Result? cached) && cached != null)
            return cached;

        try
        {
            var doc = await _users.GetByUidAsync(uid);

            // 2FA status is checked separately: database errors must fail closed rather than
            // reusing a last-known-good result that predates an enrollment change.
            bool twoFactorEnabled;
            try
            {
                await using var securityDb = await _sqlFactory.CreateDbContextAsync();
                twoFactorEnabled = await securityDb.Users.AsNoTracking()
                    .Where(x => x.Uid == uid)
                    .Select(x => x.TwoFactorEnabled)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SessionRevalidator] two-factor status lookup failed for {Uid}; rejecting session", uid);
                return new Result(false, null);
            }

            if (doc != null && doc.Role == "superadmin")
                return await ValidateSuperAdminPrincipalAsync(user);

            var twoFactorValid = !twoFactorEnabled || user.HasClaim("twoFactorVerified", "true");
            var valid = twoFactorValid && doc != null && doc.Role == "admin" && !doc.Disabled && doc.IsActive
                        && claimedBuildings.All(b => doc.BuildingIds.Contains(b));
            var result = new Result(valid, valid ? doc!.Permissions ?? new List<string>() : null);
            _cache.Set(cacheKey, result, CacheFor);
            _cache.Set(cacheKey + ":lkg", result, LastKnownGoodFor);
            return result;
        }
        catch (Exception ex)
        {
            // A principal without a verified second factor must not use a stale result
            // when the account's current 2FA policy cannot be checked.
            if (!user.HasClaim("twoFactorVerified", "true"))
            {
                _logger.LogError(ex, "[SessionRevalidator] lookup failed for {Uid} without verified 2FA; rejecting session", uid);
                return new Result(false, null);
            }

            if (_cache.TryGetValue(cacheKey + ":lkg", out Result? lkg) && lkg != null)
            {
                _logger.LogWarning(ex, "[SessionRevalidator] lookup failed for {Uid}; using last known result for a 2FA-verified session", uid);
                return lkg;
            }

            _logger.LogError(ex, "[SessionRevalidator] lookup failed for {Uid}; rejecting session", uid);
            return new Result(false, null);
        }
    }

    private static bool IsSuperAdminPrincipal(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;
        return role == "superadmin" || user.HasClaim("originalRole", "superadmin");
    }

    private async Task<Result> ValidateSuperAdminPrincipalAsync(ClaimsPrincipal user)
    {
        var uid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(uid) || !SqlAuthService.IsSuperAdminEmail(email))
            return new Result(false, null);

        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var account = await db.Users.AsNoTracking()
                .Where(x => x.Uid == uid && x.Email == email && x.Role == "superadmin")
                .Select(x => new
                {
                    x.IsActive,
                    x.IsDisabled,
                    x.TwoFactorEnabled,
                    x.CreatedAt,
                    x.UpdatedAt,
                    x.Pin,
                    Permissions = x.Permissions.Select(p => p.Permission).ToList()
                })
                .FirstOrDefaultAsync();

            var currentSecurityStamp = account == null ? null
                : SqlAuthService.CreateSuperAdminSecurityStamp(
                    account.Pin, account.UpdatedAt ?? account.CreatedAt, account.IsActive, account.IsDisabled);
            var securityStampMatches = string.Equals(
                currentSecurityStamp,
                user.FindFirst("legacySuperAdminSecurityStamp")?.Value,
                StringComparison.Ordinal);
            var valid = account != null && account.IsActive && !account.IsDisabled
                && securityStampMatches
                && (!account.TwoFactorEnabled || user.HasClaim("twoFactorVerified", "true"));
            return new Result(valid, valid ? account!.Permissions.Distinct(StringComparer.Ordinal).ToList() : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SessionRevalidator] super-admin validation failed for {Uid}; rejecting session", uid);
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
        var impersonatedBy = user.FindFirst("impersonatedBy")?.Value;

        if (string.IsNullOrWhiteSpace(actorUid) ||
            string.IsNullOrWhiteSpace(actorRole) ||
            string.IsNullOrWhiteSpace(buildingId) ||
            !string.Equals(impersonatedBy, actorUid, StringComparison.Ordinal) ||
            (actorRole == "superadmin" && string.IsNullOrWhiteSpace(actorEmail)) ||
            (actorRole == "admin" && string.IsNullOrWhiteSpace(actorPermissionsJson)) ||
            (actorRole != "admin" && actorRole != "superadmin") ||
            (effectiveRole == "resident" && string.IsNullOrWhiteSpace(apartmentId)) ||
            (effectiveRole == "admin" && !string.IsNullOrWhiteSpace(apartmentId)) ||
            (effectiveRole != "resident" && effectiveRole != "admin"))
        {
            _logger.LogWarning("[SessionRevalidator] Rejecting impersonation with incomplete actor/target claims");
            return new Result(false, null);
        }

        var actorPermissionVersion = _permissionVersions.GetOrAdd(actorUid, 0);
        var cacheKey = $"sessval:imp:{actorUid}:v:{actorPermissionVersion}:{actorRole}:{buildingId}:{apartmentId ?? "-"}:2fa:{actorTwoFactorVerified ?? "missing"}";
        if (_cache.TryGetValue(cacheKey, out Result? cached) && cached != null)
            return cached;

        try
        {
            bool actorValid;
            if (actorRole == "superadmin")
            {
                // Super-admin eligibility is currently governed by the configured allow-list.
                actorValid = SqlAuthService.IsSuperAdminEmail(actorEmail);
            }
            else
            {
                var actor = await _users.GetByUidAsync(actorUid);
                var capturedPermissions = JsonSerializer.Deserialize<List<string>>(actorPermissionsJson!) ?? new List<string>();
                var currentPermissions = actor?.Permissions?.Distinct().OrderBy(p => p).ToList() ?? new List<string>();
                var permissionsMatch = capturedPermissions.Distinct().OrderBy(p => p)
                    .SequenceEqual(currentPermissions, StringComparer.Ordinal);

                actorValid = actor != null &&
                    actor.Role == "admin" &&
                    !actor.Disabled &&
                    actor.IsActive &&
                    actor.BuildingIds.Contains(buildingId) &&
                    permissionsMatch;
            }

            if (!actorValid)
            {
                var rejected = new Result(false, null);
                _cache.Set(cacheKey, rejected, ImpersonationCacheFor);
                return rejected;
            }

            await using var db = await _sqlFactory.CreateDbContextAsync();
            var actorSecurity = await db.Users.AsNoTracking()
                .Where(u => u.Uid == actorUid && u.Role == actorRole)
                .Select(u => new { u.Email, u.IsActive, u.IsDisabled, u.TwoFactorEnabled, u.Pin, u.CreatedAt, u.UpdatedAt })
                .FirstOrDefaultAsync();

            actorValid = actorValid && actorSecurity != null
                && actorSecurity.IsActive && !actorSecurity.IsDisabled;
            if (actorRole == "superadmin")
            {
                var currentActorStamp = actorSecurity == null ? null
                    : SqlAuthService.CreateSuperAdminSecurityStamp(
                        actorSecurity.Pin, actorSecurity.UpdatedAt ?? actorSecurity.CreatedAt,
                        actorSecurity.IsActive, actorSecurity.IsDisabled);
                actorValid = actorValid && actorSecurity?.Email == actorEmail
                    && SqlAuthService.IsSuperAdminEmail(actorEmail)
                    && !string.IsNullOrWhiteSpace(actorSecurityStamp)
                    && string.Equals(currentActorStamp, actorSecurityStamp, StringComparison.Ordinal);
            }
            if (actorSecurity?.TwoFactorEnabled == true && actorTwoFactorVerified != "true")
                actorValid = false;

            if (!actorValid)
            {
                var rejected = new Result(false, null);
                _cache.Set(cacheKey, rejected, ImpersonationCacheFor);
                return rejected;
            }

            var buildingExists = await db.Buildings.AsNoTracking()
                .AnyAsync(b => b.Id == buildingId);
            if (!buildingExists)
            {
                var rejected = new Result(false, null);
                _cache.Set(cacheKey, rejected, ImpersonationCacheFor);
                return rejected;
            }

            var targetExists = string.IsNullOrWhiteSpace(apartmentId) ||
                await db.Apartments.AsNoTracking()
                    .AnyAsync(a => a.BuildingId == buildingId && a.Id == apartmentId);

            var result = new Result(targetExists, null);
            _cache.Set(cacheKey, result, ImpersonationCacheFor);
            return result;
        }
        catch (Exception ex)
        {
            // No last-known-good fallback for impersonation: an unavailable authority source
            // must not silently preserve elevated access.
            _logger.LogError(ex, "[SessionRevalidator] Impersonation validation failed for actor {ActorUid}", actorUid);
            return new Result(false, null);
        }
    }

    private async Task<Result> ValidateResidentAsync(ClaimsPrincipal user)
    {
        var uid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return new Result(false, null);

        var cacheKey = "sessval:res:" + uid;
        if (_cache.TryGetValue(cacheKey, out Result? cached) && cached != null)
            return cached;

        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var ok = await db.Residents.AsNoTracking()
                .AnyAsync(r => r.Uid == uid && r.IsActive && !r.IsDisabled);

            var result = new Result(ok, null);
            _cache.Set(cacheKey, result, CacheFor);
            _cache.Set(cacheKey + ":lkg", result, LastKnownGoodFor);
            return result;
        }
        catch (Exception ex)
        {
            if (_cache.TryGetValue(cacheKey + ":lkg", out Result? lkg) && lkg != null)
            {
                _logger.LogWarning(ex, "[SessionRevalidator] resident lookup failed for {Uid}; using last known result", uid);
                return lkg;
            }

            _logger.LogError(ex, "[SessionRevalidator] resident lookup failed for {Uid}; rejecting session", uid);
            return new Result(false, null);
        }
    }

    private async Task<Result> ValidateTargetIdentityAccountAsync(ClaimsPrincipal user)
    {
        if (user.FindFirst(ClaimTypes.Role)?.Value != "resident" ||
            !Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var accountId))
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
