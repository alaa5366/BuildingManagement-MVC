using System.Security.Claims;
using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingManagementMvc.Services;

public class SessionRevalidator
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(2);

    // لو الـ DB وقعت لحظيًا: نكمّل بآخر نتيجة سليمة لحد 15 دقيقة، وبعدها نقفل الجلسة (fail closed)
    private static readonly TimeSpan LastKnownGoodFor = TimeSpan.FromMinutes(15);

    private readonly UsersService _users;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SessionRevalidator> _logger;

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

    public sealed record Result(bool Valid, List<string>? Permissions);

    public async Task<Result> ValidateAsync(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        // ✅ جلسات Impersonation — دايماً Valid (التحقق بيتعمل عند الدخول)
        if (user.HasClaim("impersonated", "true"))
        {
            return new Result(true, null);
        }

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