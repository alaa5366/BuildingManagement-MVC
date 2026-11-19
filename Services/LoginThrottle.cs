using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingManagementMvc.Services;

/// <summary>
/// In-process fixed-window protection against credential guessing.
/// Persistence is intentionally disabled until LoginThrottle has an EF-managed
/// schema and a tested persistence contract. No database DDL/DML is performed here.
/// </summary>
public sealed class LoginThrottle
{
    private const int MaxFailuresPerAccount = 5;
    private const int MaxFailuresPerIp = 20;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private sealed class Counter
    {
        public int Count { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    private readonly IMemoryCache _cache;
    private readonly object _sync = new();

    public LoginThrottle(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Returns true when the account or source IP has reached its fixed-window limit.
    /// </summary>
    public bool IsLocked(string accountKey, string? ip)
    {
        if (Get(accountKey) >= MaxFailuresPerAccount) return true;
        return !string.IsNullOrWhiteSpace(ip) && Get("ip:" + ip) >= MaxFailuresPerIp;
    }

    /// <summary>Records one failed attempt for the account and, when present, the source IP.</summary>
    public void RegisterFailure(string accountKey, string? ip)
    {
        Bump(accountKey);
        if (!string.IsNullOrWhiteSpace(ip)) Bump("ip:" + ip);
    }

    /// <summary>Clears the account-specific counter after successful authentication.</summary>
    public void Reset(string accountKey)
    {
        lock (_sync)
            _cache.Remove(HashKey(accountKey));
    }

    private int Get(string key)
    {
        lock (_sync)
        {
            if (!_cache.TryGetValue(HashKey(key), out Counter? counter) || counter is null)
                return 0;

            if (counter.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                _cache.Remove(HashKey(key));
                return 0;
            }

            return counter.Count;
        }
    }

    private void Bump(string key)
    {
        var cacheKey = HashKey(key);
        lock (_sync)
        {
            if (!_cache.TryGetValue(cacheKey, out Counter? counter) || counter is null
                || counter.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                counter = new Counter { ExpiresAt = DateTimeOffset.UtcNow.Add(Window) };
                _cache.Set(cacheKey, counter, counter.ExpiresAt);
            }

            counter.Count++;
        }
    }

    internal static string K(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim().ToLowerInvariant()));
        return "lf:" + Convert.ToHexString(hash, 0, 16);
    }

    private static string HashKey(string key) => K(key);
}
