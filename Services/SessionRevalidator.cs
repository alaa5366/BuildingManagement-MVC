using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingManagementMvc.Services;

// بيراجع الجلسة (الكوكي) كل شوية بدل ما نثق فيها 7 أيام:
//  - admin: لو اتعطّل/اتحذف/اتغيّر دوره  => الجلسة تبطل
//  - admin: الصلاحيات (perm) بتتحدّث من Firestore من غير ما يعمل login تاني
//  - superadmin: لو إيميله اتشال من Auth:SuperAdminEmails => الجلسة تبطل
// النتيجة بتتخزّن دقيقتين عشان مانضغطش على Firestore.
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

    // Permissions = null يعني "ماتغيّرش الـ claims الحالية"
    public sealed record Result(bool Valid, List<string>? Permissions);

    public async Task<Result> ValidateAsync(ClaimsPrincipal user)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        if (user.HasClaim("impersonated", "true"))
        {
            return new Result(true, null);
        }

        if (role == "superadmin")
        {
            var ok = AuthService.IsSuperAdminEmail(user.FindFirst(ClaimTypes.Email)?.Value);
            return new Result(ok, null);
        }

        // جلسات impersonation (resident) و QR مالهاش مستند أدمن
        if (role != "admin") return new Result(true, null);

        var uid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return new Result(false, null);

        var cacheKey = "sessval:" + uid;
        if (_cache.TryGetValue(cacheKey, out Result? cached) && cached != null)
            return cached;

        try
        {
            var doc = await _users.GetByUidAsync(uid);
            var valid = doc != null && doc.Role == "admin" && !doc.Disabled && doc.IsActive;
            var result = new Result(valid, valid ? doc!.Permissions ?? new List<string>() : null);
            _cache.Set(cacheKey, result, CacheFor);
            return result;
        }
        catch (Exception ex)
        {
            // لو Firestore وقع مؤقتاً مانطرّدش كل الأدمنز — وماننسّخش الفشل في الكاش
            _logger.LogWarning(ex, "[SessionRevalidator] lookup failed for {Uid}; keeping session", uid);
            return new Result(true, null);
        }
    }
}
