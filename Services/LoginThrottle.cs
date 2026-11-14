using System.Security.Cryptography;
using System.Text;
using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingManagementMvc.Services;

/// <summary>
/// حماية من تخمين الـ PIN / كلمة السر.
/// </summary>
/// <remarks>
/// <para>
/// <b>القواعد:</b>
/// </para>
/// <list type="bullet">
///   <item>5 محاولات غلط لنفس الحساب ⇒ قفل 15 دقيقة.</item>
///   <item>20 محاولة غلط من نفس الـ IP ⇒ قفل 15 دقيقة.</item>
/// </list>
/// <para>
/// <b>التخزين:</b> العدّاد شغّال في <see cref="IMemoryCache"/> (سريع)، ومتسجّل كمان
/// في جدول <c>dbo.LoginThrottle</c>، فبيفضل شغال بعد أي restart.
/// </para>
/// <para>
/// <b>⚠️ التزامن:</b> التزامن بين أكتر من instance مش مدعوم (كل instance بيقرا
/// الحالة وقت الإقلاع).
/// </para>
/// </remarks>
/// <seealso cref="InitializeAsync"/>
/// <seealso cref="IsLocked"/>
/// <seealso cref="RegisterFailure"/>
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

    /// <summary>
    /// صف واحد من جدول <c>dbo.LoginThrottle</c>.
    /// </summary>
    public sealed class ThrottleRow
    {
        /// <summary>المفتاح المشفّر (SHA-256).</summary>
        public string Key { get; set; } = "";

        /// <summary>عدد المحاولات الفاشلة.</summary>
        public int Count { get; set; }

        /// <summary>تاريخ الانتهاء (UTC).</summary>
        public DateTime ExpiresAt { get; set; }
    }

    private readonly IMemoryCache _cache;
    private readonly IDbContextFactory<AppDbContext>? _sqlFactory;
    private readonly ILogger<LoginThrottle>? _log;
    private readonly object _lock = new();
    private volatile bool _persist;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginThrottle"/> class.
    /// </summary>
    /// <param name="cache">الـ memory cache (للتخزين السريع).</param>
    /// <param name="sqlFactory">Factory للـ DB (اختياري — للـ persistence).</param>
    /// <param name="log">الـ logger (اختياري).</param>
    public LoginThrottle(
        IMemoryCache cache,
        IDbContextFactory<AppDbContext>? sqlFactory = null,
        ILogger<LoginThrottle>? log = null)
    {
        _cache = cache;
        _sqlFactory = sqlFactory;
        _log = log;
    }

    /// <summary>
    /// يهيّئ الحالة من الـ DB عند الإقلاع.
    /// </summary>
    /// <returns>Task مكتملة بعد التهيئة.</returns>
    /// <remarks>
    /// بيتنادى مرة واحدة في <c>Program.cs</c> بعد <c>app.Build()</c>.
    /// </remarks>
    public async Task InitializeAsync()
    {
        if (_sqlFactory == null) return;

        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();

            await db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.LoginThrottle', N'U') IS NULL
    CREATE TABLE dbo.LoginThrottle (
        [Key] nvarchar(80) NOT NULL PRIMARY KEY,
        [Count] int NOT NULL,
        ExpiresAt datetime2 NOT NULL
    );
DELETE FROM dbo.LoginThrottle WHERE ExpiresAt < SYSUTCDATETIME();");

            var rows = await db.Database
                .SqlQuery<ThrottleRow>($"SELECT [Key], [Count], ExpiresAt FROM dbo.LoginThrottle")
                .ToListAsync();

            foreach (var r in rows)
            {
                var exp = new DateTimeOffset(DateTime.SpecifyKind(r.ExpiresAt, DateTimeKind.Utc));
                if (exp <= DateTimeOffset.UtcNow) continue;
                _cache.Set(r.Key, new Counter { Count = r.Count, ExpiresAt = exp }, exp);
            }

            _persist = true;
            _log?.LogInformation("[LoginThrottle] persistent mode on; restored {N} counters", rows.Count);
        }
        catch (Exception ex)
        {
            _persist = false;
            _log?.LogWarning(ex, "[LoginThrottle] could not use the database; staying in-memory only");
        }
    }

    /// <summary>
    /// يحوّل المفتاح لـ SHA-256 hash (عشان ما نخزّنش أرقام تليفونات/إيميلات صريحة).
    /// </summary>
    /// <param name="key">المفتاح الأصلي (مثال: <c>"acct:+201001234567"</c>).</param>
    /// <returns>Hash يبدأ بـ <c>lf:</c> + 32 حرف hex.</returns>
    internal static string K(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key.Trim().ToLowerInvariant()));
        return "lf:" + Convert.ToHexString(hash, 0, 16);
    }

    private int Get(string key) =>
        _cache.TryGetValue(K(key), out Counter? c) && c != null ? c.Count : 0;

    /// <summary>
    /// بيتحقق لو الحساب أو الـ IP مقفول.
    /// </summary>
    /// <param name="accountKey">مفتاح الحساب (UID، إيميل، أو رقم تليفون).</param>
    /// <param name="ip">عنوان IP (اختياري).</param>
    /// <returns><c>true</c> لو مقفول.</returns>
    public bool IsLocked(string accountKey, string? ip)
    {
        if (Get(accountKey) >= MaxFailuresPerAccount) return true;
        return !string.IsNullOrEmpty(ip) && Get("ip:" + ip) >= MaxFailuresPerIp;
    }

    /// <summary>
    /// يسجّل محاولة فاشلة (بيزود العدّاد).
    /// </summary>
    /// <param name="accountKey">مفتاح الحساب.</param>
    /// <param name="ip">عنوان IP (اختياري).</param>
    public void RegisterFailure(string accountKey, string? ip)
    {
        Bump(accountKey);
        if (!string.IsNullOrEmpty(ip)) Bump("ip:" + ip);
    }

    /// <summary>
    /// يمسح عدّاد الحساب (بيتنادى عند الدخول الناجح).
    /// </summary>
    /// <param name="accountKey">مفتاح الحساب.</param>
    public void Reset(string accountKey)
    {
        var k = K(accountKey);
        _cache.Remove(k);
        if (_persist) FireAndForget(() => DeleteAsync(k));
    }

    private void Bump(string key)
    {
        var k = K(key);
        int count;
        DateTimeOffset exp;

        lock (_lock)
        {
            if (!_cache.TryGetValue(k, out Counter? c) || c == null)
            {
                c = new Counter { ExpiresAt = DateTimeOffset.UtcNow + Window };
                _cache.Set(k, c, c.ExpiresAt);
            }
            c.Count++;
            count = c.Count;
            exp = c.ExpiresAt;
        }

        if (_persist) FireAndForget(() => UpsertAsync(k, count, exp));
    }

    private void FireAndForget(Func<Task> work) =>
        _ = Task.Run(async () =>
        {
            try { await work(); }
            catch (Exception ex) { _log?.LogWarning(ex, "[LoginThrottle] persist failed"); }
        });

    private async Task UpsertAsync(string k, int count, DateTimeOffset exp)
    {
        if (_sqlFactory == null) return;
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var expUtc = exp.UtcDateTime;
        await db.Database.ExecuteSqlInterpolatedAsync($@"
MERGE dbo.LoginThrottle AS t
USING (SELECT {k} AS [Key]) AS s ON t.[Key] = s.[Key]
WHEN MATCHED THEN UPDATE SET [Count] = {count}, ExpiresAt = {expUtc}
WHEN NOT MATCHED THEN INSERT ([Key], [Count], ExpiresAt) VALUES ({k}, {count}, {expUtc});");
    }

    private async Task DeleteAsync(string k)
    {
        if (_sqlFactory == null) return;
        await using var db = await _sqlFactory.CreateDbContextAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.LoginThrottle WHERE [Key] = {k}");
    }
}