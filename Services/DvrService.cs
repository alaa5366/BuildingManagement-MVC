using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class DvrService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IEncryptionService _enc;
    private readonly ILogger<DvrService> _log;

    public DvrService(
        IDbContextFactory<AppDbContext> sqlFactory,
        IEncryptionService enc,
        ILogger<DvrService> log)
    {
        _sqlFactory = sqlFactory;
        _enc = enc;
        _log = log;
    }

    public async Task<List<DvrEntity>> GetByBuildingAsync(string buildingId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        return await db.Dvrs
            .Include(d => d.Cameras)
            .Where(d => d.BuildingId == buildingId)
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .AsSplitQuery()
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

        await using var db = await _sqlFactory.CreateDbContextAsync();
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

        return id;
    }

    public async Task UpdateAsync(
        string id, string name, string ip, int port,
        string brand, string username, string? newPassword, bool isActive)
    {
        name = string.IsNullOrWhiteSpace(name) ? "DVR" : name.Trim();
        ip = string.IsNullOrWhiteSpace(ip) ? "127.0.0.1" : ip.Trim();
        brand = string.IsNullOrWhiteSpace(brand) ? "Other" : brand.Trim();
        username = username?.Trim() ?? "";

        await using var db = await _sqlFactory.CreateDbContextAsync();
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

    public async Task DeleteAsync(string id)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        await db.Dvrs.Where(d => d.Id == id).ExecuteDeleteAsync();
    }

    public async Task<string> AddCameraAsync(string dvrId, string name, int channel, string rtspPath)
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

        return id;
    }

    public async Task DeleteCameraAsync(string cameraId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        await db.Cameras.Where(c => c.Id == cameraId).ExecuteDeleteAsync();
    }

    public async Task<string> GetDecryptedPasswordAsync(string dvrId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var dvr = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == dvrId)
            ?? throw new KeyNotFoundException();
        return _enc.Decrypt(dvr.PasswordEncrypted);
    }

    public async Task<SyncDvrReport> SyncAllToSqlAsync()
    {
        // بعد إلغاء Firebase، مفيش حاجة تتزامن
        return new SyncDvrReport
        {
            Elapsed = TimeSpan.Zero,
            Errors = new List<string> { "Firestore removed — no sync needed" }
        };
    }
}

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