// =====================================================================
//  StorageSettingsService - إدارة وضع التخزين في Runtime (محصّن)
//
//  الأوضاع المتاحة:
//  - Firestore : قراءة وكتابة من Firestore فقط
//  - Sql       : قراءة وكتابة من SQL Server فقط
//  - Dual      : قراءة من Firestore، كتابة على الاثنين
//
//  الـ Cache: 5 دقائق
//  القراءة: SQL أولاً (أسرع) ثم Firestore (احتياطي)
// =====================================================================
using BuildingManagementMvc.Data;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class StorageSettingsService
{
    private readonly FirestoreDb _db;
    private readonly IDbContextFactory<AppDbContext>? _sqlFactory;
    private readonly ILogger<StorageSettingsService> _log;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedMode;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private const string Col = "system_settings";
    private const string DocId = "storage";

    public StorageSettingsService(
        FirestoreContext ctx,
        ILogger<StorageSettingsService> log,
        IDbContextFactory<AppDbContext>? sqlFactory = null)
    {
        _db = ctx.Db;
        _log = log;
        _sqlFactory = sqlFactory;
    }

    /// <summary>يقرأ الوضع الحالي. الافتراضي: "Dual".</summary>
    public async Task<string> GetModeAsync()
    {
        if (_cachedMode != null && DateTime.UtcNow < _cacheExpiry)
            return _cachedMode;

        await _lock.WaitAsync();
        try
        {
            if (_cachedMode != null && DateTime.UtcNow < _cacheExpiry)
                return _cachedMode;

            // 1. جرّب SQL الأول
            if (_sqlFactory != null)
            {
                try
                {
                    await using var db = await _sqlFactory.CreateDbContextAsync();
                    var gs = await db.GlobalSettings.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == "global");
                    if (gs != null && !string.IsNullOrWhiteSpace(gs.StorageMode)
                        && gs.StorageMode is "Firestore" or "Sql" or "Dual")
                    {
                        _cachedMode = gs.StorageMode;
                        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
                        return gs.StorageMode;
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "SQL read of StorageMode failed");
                }
            }

            // 2. جرّب Firestore
            try
            {
                var doc = await _db.Collection(Col).Document(DocId).GetSnapshotAsync();
                var mode = (doc.Exists && doc.ContainsField("mode"))
                    ? doc.GetValue<string>("mode")
                    : "Dual";

                if (mode is not ("Firestore" or "Sql" or "Dual"))
                    mode = "Dual";

                _cachedMode = mode;
                _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);
                return mode;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Firestore read of StorageMode failed");
            }

            // 3. Fallback
            return _cachedMode ?? "Dual";
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>يحدّث الوضع في SQL و Firestore معًا.</summary>
    public async Task SetModeAsync(string mode, string userId)
    {
        if (mode is not ("Firestore" or "Sql" or "Dual"))
            throw new ArgumentException($"وضع غير صالح: {mode}", nameof(mode));

        await _lock.WaitAsync();
        try
        {
            // 1. اكتب في SQL
            if (_sqlFactory != null)
            {
                try
                {
                    await using var db = await _sqlFactory.CreateDbContextAsync();
                    var gs = await db.GlobalSettings.FirstOrDefaultAsync(x => x.Id == "global");
                    if (gs != null)
                    {
                        gs.StorageMode = mode;
                        gs.UpdatedAt = DateTime.UtcNow;
                        gs.UpdatedBy = userId;
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "SQL write of StorageMode failed");
                }
            }

            // 2. اكتب في Firestore
            try
            {
                await _db.Collection(Col).Document(DocId).SetAsync(
                    new Dictionary<string, object>
                    {
                        ["mode"] = mode,
                        ["updatedAt"] = DateTime.UtcNow.ToString("o"),
                        ["updatedBy"] = userId
                    },
                    SetOptions.MergeAll);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Firestore write of StorageMode failed (متوقع لو Firebase وقع)");
            }

            // 3. حدّث الـ Cache
            _cachedMode = mode;
            _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

            _log.LogWarning("Storage mode changed to {Mode} by {User}", mode, userId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void InvalidateCache()
    {
        _cachedMode = null;
        _cacheExpiry = DateTime.MinValue;
    }
}