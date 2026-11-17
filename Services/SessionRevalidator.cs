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
    /// Validates a signed-in principal against the database. Returns the refreshed permissions for admins, or rejects the session when the account was disabled or removed.
    /// </summary>
    public async Task<Result> ValidateAsync(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        // Impersonated principals are validated against the initiating account and target scope.
        // Synthetic resident identities must never be treated as ordinary resident database users.
        if (user.HasClaim("impersonated", "true"))
            return await ValidateImpersonatedAsync(user);

        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (role == "superadmin" || SqlAuthService.IsSuperAdminEmail(email)
            || user.HasClaim("originalRole", "superadmin"))
            return await ValidateSuperAdminPrincipalAsync(user);

        // ✅ الساكن (دخول عادي أو QR): لازم لسه موجود ونشط وغير معطّل
        if (role == "resident")
            return await ValidateResidentAsync(user);

        if (role != "admin") return new Result(true, null);

        var uid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return new Result(false, null);

        var claimedBuildings = user.FindAll("buildingId").Select(c => c.Value).OrderBy(x => x).ToList();
        var twoFactorClaim = user.FindFirst("twoFactorVerified")?.Value ?? "missing";
        var cacheKey = "sessval:" + uid + ":" + string.Join(",", claimedBuildings) + ":2fa:" + twoFactorClaim;
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
            if (_cache.TryGetValue(cacheKey + ":lkg", out Result? lkg) && lkg != null)
            {
                _logger.LogWarning(ex, "[SessionRevalidator] lookup failed for {Uid}; using last known result", uid);
                return lkg;
            }

            _logger.LogError(ex, "[SessionRevalidator] lookup failed for {Uid}; rejecting session", uid);
            return new Result(false, null);
        }
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
                .Select(x => new { x.IsActive, x.IsDisabled, x.TwoFactorEnabled })
                .FirstOrDefaultAsync();

            var valid = account != null && account.IsActive && !account.IsDisabled
                && (!account.TwoFactorEnabled || user.HasClaim("twoFactorVerified", "true"));
            return new Result(valid, null);
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

        var cacheKey = $"sessval:imp:{actorUid}:{actorRole}:{buildingId}:{apartmentId ?? "-"}:2fa:{actorTwoFactorVerified ?? "missing"}";
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
                .Select(u => new { u.Email, u.IsActive, u.IsDisabled, u.TwoFactorEnabled })
                .FirstOrDefaultAsync();

            actorValid = actorValid && actorSecurity != null
                && actorSecurity.IsActive && !actorSecurity.IsDisabled;
            if (actorRole == "superadmin")
                actorValid = actorValid && actorSecurity?.Email == actorEmail
                    && SqlAuthService.IsSuperAdminEmail(actorEmail);
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
}