using Microsoft.Extensions.Caching.Memory;

namespace BuildingManagementMvc.Services;

// حماية بسيطة من تخمين الـ PIN / كلمة السر:
//  - 5 محاولات غلط لنفس الحساب  => قفل 15 دقيقة
//  - 20 محاولة غلط من نفس الـ IP => قفل 15 دقيقة
// العدّاد في الذاكرة (بيتصفّر مع أي restart) — كافي لـ instance واحدة.
public class LoginThrottle
{
    private const int MaxFailuresPerAccount = 5;
    private const int MaxFailuresPerIp = 20;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private sealed class Counter
    {
        public int Count;
        public DateTimeOffset ExpiresAt;
    }

    private readonly IMemoryCache _cache;
    private readonly object _lock = new();

    public LoginThrottle(IMemoryCache cache) => _cache = cache;

    private static string K(string key) => "loginfail:" + key.Trim().ToLowerInvariant();

    private int Get(string key) =>
        _cache.TryGetValue(K(key), out Counter? c) && c != null ? c.Count : 0;

    public bool IsLocked(string accountKey, string? ip)
    {
        if (Get(accountKey) >= MaxFailuresPerAccount) return true;
        return !string.IsNullOrEmpty(ip) && Get("ip:" + ip) >= MaxFailuresPerIp;
    }

    public void RegisterFailure(string accountKey, string? ip)
    {
        Bump(accountKey);
        if (!string.IsNullOrEmpty(ip)) Bump("ip:" + ip);
    }

    public void Reset(string accountKey) => _cache.Remove(K(accountKey));

    private void Bump(string key)
    {
        lock (_lock)
        {
            var k = K(key);
            if (!_cache.TryGetValue(k, out Counter? c) || c == null)
            {
                c = new Counter { ExpiresAt = DateTimeOffset.UtcNow + Window };
                _cache.Set(k, c, c.ExpiresAt);
            }
            c.Count++;
        }
    }
}
