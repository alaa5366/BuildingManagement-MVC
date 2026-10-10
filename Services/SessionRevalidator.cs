using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingManagementMvc.Services;

public class SessionRevalidator
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(2);

    private readonly UsersService _users;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SessionRevalidator> _logger;

    public SessionRevalidator(UsersService users, IMemoryCache cache, ILogger<SessionRevalidator> logger)
    {
        _users = users;
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

        // جلسات resident و QR مالهاش مستند أدمن
        if (role != "admin") return new Result(true, null);

        var uid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return new Result(false, null);

        var cacheKey = "sessval:" + uid;
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

            var valid = doc != null && doc.Role == "admin" && !doc.Disabled && doc.IsActive;
            var result = new Result(valid, valid ? doc!.Permissions ?? new List<string>() : null);
            _cache.Set(cacheKey, result, CacheFor);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SessionRevalidator] lookup failed for {Uid}; keeping session", uid);
            return new Result(true, null);
        }
    }
}