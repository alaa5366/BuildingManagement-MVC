using System.Security.Claims;
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

        // ✅ SuperAdmin — تحقق من الإيميل
        if (role == "superadmin")
        {
            var ok = SqlAuthService.IsSuperAdminEmail(user.FindFirst(ClaimTypes.Email)?.Value);
            return new Result(ok, null);
        }

        // ✅ لو كان SuperAdmin (في حالة Impersonation Exit)
        // بنتحقق من الإيميل + originalRole
        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        if (SqlAuthService.IsSuperAdminEmail(email))
        {
            return new Result(true, null);
        }

        if (user.HasClaim("originalRole", "superadmin"))
        {
            return new Result(true, null);
        }

        // ✅ الساكن (دخول عادي أو QR): لازم لسه موجود ونشط وغير معطّل
        if (role == "resident")
            return await ValidateResidentAsync(user);

        if (role != "admin") return new Result(true, null);

        var uid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return new Result(false, null);

        var claimedBuildings = user.FindAll("buildingId").Select(c => c.Value).OrderBy(x => x).ToList();
        var cacheKey = "sessval:" + uid + ":" + string.Join(",", claimedBuildings);
        if (_cache.TryGetValue(cacheKey, out Result? cached) && cached != null)
            return cached;

        try
        {
            var doc = await _users.GetByUidAsync(uid);

            // ✅ لو Uid ده SuperAdmin (حتى لو Role=admin)
            if (doc != null && doc.Role == "superadmin")
            {
                var superResult = new Result(true, null);
                _cache.Set(cacheKey, superResult, CacheFor);
                return superResult;
            }

            // الأدمن لازم يكون لسه عضو في العمارة اللي الجلسة مربوطة بيها
            var valid = doc != null && doc.Role == "admin" && !doc.Disabled && doc.IsActive
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

    private async Task<Result> ValidateImpersonatedAsync(ClaimsPrincipal user)
    {
        var actorUid = user.FindFirst("impersonationActorUid")?.Value;
        var actorRole = user.FindFirst("impersonationActorRole")?.Value;
        var actorEmail = user.FindFirst("impersonationActorEmail")?.Value;
        var buildingId = user.FindFirst("impersonationTargetBuildingId")?.Value;
        var apartmentId = user.FindFirst("impersonationTargetApartmentId")?.Value;

        if (string.IsNullOrWhiteSpace(actorUid) ||
            string.IsNullOrWhiteSpace(actorRole) ||
            string.IsNullOrWhiteSpace(buildingId) ||
            (actorRole == "superadmin" && string.IsNullOrWhiteSpace(actorEmail)) ||
            (actorRole != "admin" && actorRole != "superadmin"))
        {
            _logger.LogWarning("[SessionRevalidator] Rejecting impersonation with incomplete actor/target claims");
            return new Result(false, null);
        }

        var cacheKey = $"sessval:imp:{actorUid}:{actorRole}:{buildingId}:{apartmentId ?? "-"}";
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
                actorValid = actor != null &&
                    actor.Role == "admin" &&
                    !actor.Disabled &&
                    actor.IsActive &&
                    actor.BuildingIds.Contains(buildingId);
            }

            if (!actorValid)
            {
                var rejected = new Result(false, null);
                _cache.Set(cacheKey, rejected, ImpersonationCacheFor);
                return rejected;
            }

            await using var db = await _sqlFactory.CreateDbContextAsync();
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