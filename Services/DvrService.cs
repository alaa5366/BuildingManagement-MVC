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

        // ✅ حماية من null
        name = string.IsNullOrWhiteSpace(name) ? "DVR" : name.Trim();
        ip = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
        brand = string.IsNullOrWhiteSpace(brand) ? "Other" : brand.Trim();
        username = username?.Trim() ?? "";
        password = password ?? "";

        // 1) SQL
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
                Username = username,                          // ✅ مش هيبقى null
                PasswordEncrypted = _enc.Encrypt(password),
                IsActive = true,
                CreatedAt = now
            });
            await db.SaveChangesAsync();
        }

        // 2) Firestore
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
        // ✅ حماية من null
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
            entity.Username = username;                       // ✅ مش هيبقى null
            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(newPassword))
                entity.PasswordEncrypted = _enc.Encrypt(newPassword);

            await db.SaveChangesAsync();
        }

        // Firestore
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
        // ✅ حماية من null
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
}