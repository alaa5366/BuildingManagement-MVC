using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class DvrService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly FirestoreDb _fs;
    private readonly IEncryptionService _enc;
    private readonly ILogger<DvrService> _log;

    private CollectionReference DvrsCol => _fs.Collection("dvrs");
    private CollectionReference CamerasCol => _fs.Collection("cameras");

    public DvrService(
        IDbContextFactory<AppDbContext> sqlFactory,
        FirestoreContext fsCtx,
        IEncryptionService enc,
        ILogger<DvrService> log)
    {
        _sqlFactory = sqlFactory;
        _fs = fsCtx.Db;
        _enc = enc;
        _log = log;
    }

    // ============================================================
    // القراءة
    // ============================================================
    public async Task<List<DvrEntity>> GetByBuildingAsync(string buildingId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        return await db.Dvrs
            .Include(d => d.Cameras)
            .Where(d => d.BuildingId == buildingId)
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<DvrEntity?> GetByIdAsync(string id)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        return await db.Dvrs
            .Include(d => d.Cameras)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    // ============================================================
    // الإنشاء
    // ============================================================
    public async Task<string> CreateAsync(
        string buildingId, string name, string ip, int port,
        string brand, string username, string password)
    {
        var id = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        name = string.IsNullOrWhiteSpace(name) ? "DVR" : name.Trim();
        ip = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
        brand = string.IsNullOrWhiteSpace(brand) ? "Other" : brand.Trim();
        username = username?.Trim() ?? "";
        password = password ?? "";

        await using (var db = await _sqlFactory.CreateDbContextAsync())
        {
            db.Dvrs.Add(new DvrEntity
            {
                Id = id,
                BuildingId = buildingId,
                Name = name,
                IpAddress = ip,
                Port = port,
                Brand = brand,
                Username = username,
                PasswordEncrypted = _enc.Encrypt(password),
                IsActive = true,
                CreatedAt = now
            });
            await db.SaveChangesAsync();
        }

        try
        {
            await DvrsCol.Document(id).SetAsync(new Dictionary<string, object>
            {
                ["buildingId"] = buildingId,
                ["name"] = name,
                ["ip"] = ip,
                ["port"] = port,
                ["brand"] = brand,
                ["username"] = username,
                ["isActive"] = true,
                ["createdAt"] = now.ToString("o")
            });
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Firestore mirror failed for DVR {Id}", id);
        }

        return id;
    }

    // ============================================================
    // التعديل
    // ============================================================
    public async Task UpdateAsync(
        string id, string name, string ip, int port,
        string brand, string username, string? newPassword, bool isActive)
    {
        name = string.IsNullOrWhiteSpace(name) ? "DVR" : name.Trim();
        ip = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
        brand = string.IsNullOrWhiteSpace(brand) ? "Other" : brand.Trim();
        username = username?.Trim() ?? "";

        await using (var db = await _sqlFactory.CreateDbContextAsync())
        {
            var entity = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new KeyNotFoundException($"DVR {id} not found");

            entity.Name = name;
            entity.IpAddress = ip;
            entity.Port = port;
            entity.Brand = brand;
            entity.Username = username;
            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(newPassword))
                entity.PasswordEncrypted = _enc.Encrypt(newPassword);

            await db.SaveChangesAsync();
        }

        try
        {
            await DvrsCol.Document(id).SetAsync(new Dictionary<string, object>
            {
                ["name"] = name,
                ["ip"] = ip,
                ["port"] = port,
                ["brand"] = brand,
                ["username"] = username,
                ["isActive"] = isActive
            }, SetOptions.MergeAll);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Firestore update failed for DVR {Id}", id);
        }
    }

    // ============================================================
    // الحذف
    // ============================================================
    public async Task DeleteAsync(string id)
    {
        await using (var db = await _sqlFactory.CreateDbContextAsync())
        {
            await db.Dvrs.Where(d => d.Id == id).ExecuteDeleteAsync();
        }

        try
        {
            await DvrsCol.Document(id).DeleteAsync();
            var cams = await CamerasCol.WhereEqualTo("dvrId", id).GetSnapshotAsync();
            foreach (var c in cams.Documents)
                await c.Reference.DeleteAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Firestore delete failed for DVR {Id}", id);
        }
    }

    // ============================================================
    // الكاميرات
    // ============================================================
    public async Task<string> AddCameraAsync(
        string dvrId, string name, int channel, string rtspPath)
    {
        name = string.IsNullOrWhiteSpace(name) ? $"Camera {channel}" : name.Trim();
        rtspPath = rtspPath?.Trim() ?? "";

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var dvr = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == dvrId)
            ?? throw new KeyNotFoundException($"DVR {dvrId} not found");

        var id = Guid.NewGuid().ToString("N");
        db.Cameras.Add(new CameraEntity
        {
            Id = id,
            DvrId = dvrId,
            BuildingId = dvr.BuildingId,
            Name = name,
            Channel = channel,
            RtspPath = rtspPath,
            IsActive = true
        });
        await db.SaveChangesAsync();

        try
        {
            await CamerasCol.Document(id).SetAsync(new Dictionary<string, object>
            {
                ["dvrId"] = dvrId,
                ["buildingId"] = dvr.BuildingId,
                ["name"] = name,
                ["channel"] = channel,
                ["isActive"] = true,
                ["createdAt"] = DateTime.UtcNow.ToString("o")
            });
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Firestore mirror failed for Camera {Id}", id);
        }

        return id;
    }

    public async Task DeleteCameraAsync(string cameraId)
    {
        await using (var db = await _sqlFactory.CreateDbContextAsync())
        {
            await db.Cameras.Where(c => c.Id == cameraId).ExecuteDeleteAsync();
        }

        try
        {
            await CamerasCol.Document(cameraId).DeleteAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Firestore delete failed for Camera {Id}", cameraId);
        }
    }

    public async Task<string> GetDecryptedPasswordAsync(string dvrId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var dvr = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == dvrId)
            ?? throw new KeyNotFoundException();
        return _enc.Decrypt(dvr.PasswordEncrypted);
    }

    // ============================================================
    // للمزامنة العكسية
    // ============================================================
    public async Task SyncOneToSqlAsync(DvrDoc doc)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entity = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == doc.Id);
        if (entity == null)
        {
            entity = new DvrEntity { Id = doc.Id };
            db.Dvrs.Add(entity);
        }
        entity.BuildingId = doc.BuildingId;
        entity.Name = doc.Name;
        entity.IpAddress = doc.IpAddress;
        entity.Port = doc.Port;
        entity.Brand = doc.Brand;
        entity.Username = doc.Username ?? "";
        entity.IsActive = doc.IsActive;
        await db.SaveChangesAsync();
    }

    // ============================================================
    // ✅ المزامنة الكاملة: Firestore → SQL
    // ============================================================
    public async Task<SyncDvrReport> SyncAllToSqlAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var report = new SyncDvrReport();

        try
        {
            var dvrsSnapshot = await DvrsCol.GetSnapshotAsync();
            _log.LogInformation("Found {Count} DVRs in Firestore", dvrsSnapshot.Documents.Count);

            await using var db = await _sqlFactory.CreateDbContextAsync();

            foreach (var doc in dvrsSnapshot.Documents)
            {
                try
                {
                    var data = doc.ToDictionary();

                    var id = doc.Id;
                    var buildingId = GetString(data, "buildingId");
                    var name = GetString(data, "name", "DVR");
                    var ip = GetString(data, "ip");
                    var port = GetInt(data, "port", 554);
                    var brand = GetString(data, "brand", "Other");
                    var username = GetString(data, "username");
                    var isActive = GetBool(data, "isActive", true);
                    var createdAtStr = GetString(data, "createdAt");

                    if (string.IsNullOrWhiteSpace(buildingId))
                    {
                        report.Errors.Add($"DVR {id}: buildingId مفقود - اتخطّى");
                        continue;
                    }

                    var buildingExists = await db.Buildings.AnyAsync(b => b.Id == buildingId);
                    if (!buildingExists)
                    {
                        report.Errors.Add($"DVR {id}: العمارة '{buildingId}' مش موجودة في SQL - اتخطّى");
                        continue;
                    }

                    var existing = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == id);
                    if (existing == null)
                    {
                        db.Dvrs.Add(new DvrEntity
                        {
                            Id = id,
                            BuildingId = buildingId,
                            Name = name,
                            IpAddress = ip,
                            Port = port,
                            Brand = brand,
                            Username = username,
                            PasswordEncrypted = "",
                            IsActive = isActive,
                            CreatedAt = ParseTs(createdAtStr) ?? DateTime.UtcNow
                        });
                        report.DvrsAdded++;
                    }
                    else
                    {
                        existing.BuildingId = buildingId;
                        existing.Name = name;
                        existing.IpAddress = ip;
                        existing.Port = port;
                        existing.Brand = brand;
                        existing.Username = username;
                        existing.IsActive = isActive;
                        existing.UpdatedAt = DateTime.UtcNow;
                        report.DvrsUpdated++;
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add($"DVR {doc.Id}: {ex.Message}");
                    _log.LogWarning(ex, "Failed to sync DVR {Id}", doc.Id);
                }
            }

            await db.SaveChangesAsync();

            var camerasSnapshot = await CamerasCol.GetSnapshotAsync();
            _log.LogInformation("Found {Count} cameras in Firestore", camerasSnapshot.Documents.Count);

            foreach (var doc in camerasSnapshot.Documents)
            {
                try
                {
                    var data = doc.ToDictionary();

                    var id = doc.Id;
                    var dvrId = GetString(data, "dvrId");
                    var buildingId = GetString(data, "buildingId");
                    var name = GetString(data, "name", "Camera");
                    var channel = GetInt(data, "channel", 1);
                    var rtspPath = GetString(data, "rtspPath");
                    var hlsUrl = GetString(data, "hlsUrl");
                    var isActive = GetBool(data, "isActive", true);
                    var createdAtStr = GetString(data, "createdAt");

                    if (string.IsNullOrWhiteSpace(dvrId))
                    {
                        report.Errors.Add($"Camera {id}: dvrId مفقود - اتخطّت");
                        continue;
                    }

                    var dvrExists = await db.Dvrs.AnyAsync(d => d.Id == dvrId);
                    if (!dvrExists)
                    {
                        report.Errors.Add($"Camera {id}: DVR '{dvrId}' مش موجود في SQL - اتخطّت");
                        continue;
                    }

                    var existing = await db.Cameras.FirstOrDefaultAsync(c => c.Id == id);
                    if (existing == null)
                    {
                        db.Cameras.Add(new CameraEntity
                        {
                            Id = id,
                            DvrId = dvrId,
                            BuildingId = buildingId,
                            Name = name,
                            Channel = channel,
                            RtspPath = rtspPath,
                            HlsUrl = hlsUrl,
                            IsActive = isActive,
                            CreatedAt = ParseTs(createdAtStr) ?? DateTime.UtcNow
                        });
                        report.CamerasAdded++;
                    }
                    else
                    {
                        existing.DvrId = dvrId;
                        existing.BuildingId = buildingId;
                        existing.Name = name;
                        existing.Channel = channel;
                        existing.RtspPath = rtspPath;
                        existing.HlsUrl = hlsUrl;
                        existing.IsActive = isActive;
                        report.CamerasUpdated++;
                    }
                }
                catch (Exception ex)
                {
                    report.Errors.Add($"Camera {doc.Id}: {ex.Message}");
                    _log.LogWarning(ex, "Failed to sync Camera {Id}", doc.Id);
                }
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            report.Errors.Add($"خطأ عام: {ex.Message}");
            _log.LogError(ex, "SyncAllToSqlAsync failed");
        }

        sw.Stop();
        report.Elapsed = sw.Elapsed;
        return report;
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static string GetString(Dictionary<string, object> d, string key, string def = "")
        => d.TryGetValue(key, out var v) && v != null ? v.ToString() ?? def : def;

    private static int GetInt(Dictionary<string, object> d, string key, int def)
        => d.TryGetValue(key, out var v) && v != null && int.TryParse(v.ToString(), out var i) ? i : def;

    private static bool GetBool(Dictionary<string, object> d, string key, bool def)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return def;
        if (v is bool b) return b;
        return bool.TryParse(v.ToString(), out var parsed) ? parsed : def;
    }

    private static DateTime? ParseTs(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParse(s, null,
                System.Globalization.DateTimeStyles.AdjustToUniversal |
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var dt))
            return new DateTime(dt.Ticks - dt.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        return null;
    }
}

// ============================================================
// SyncDvrReport
// ============================================================
public class SyncDvrReport
{
    public int DvrsAdded { get; set; }
    public int DvrsUpdated { get; set; }
    public int CamerasAdded { get; set; }
    public int CamerasUpdated { get; set; }
    public TimeSpan Elapsed { get; set; }
    public List<string> Errors { get; set; } = new();

    public int DvrsTotal => DvrsAdded + DvrsUpdated;
    public int CamerasTotal => CamerasAdded + CamerasUpdated;
}