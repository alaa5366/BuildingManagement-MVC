using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Business logic for DVR devices and cameras, including encryption of stored credentials.
/// </summary>
public class DvrService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IEncryptionService _enc;
    private readonly ILogger<DvrService> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="DvrService"/> class.
    /// </summary>
    public DvrService(
        IDbContextFactory<AppDbContext> sqlFactory,
        IEncryptionService enc,
        ILogger<DvrService> log)
    {
        _sqlFactory = sqlFactory;
        _enc = enc;
        _log = log;
    }

    public async Task< DvrBuildingSummary?> GetBuildingSummaryAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync(cancellationToken);
        var building = await db.Buildings.AsNoTracking()
            .Where(x => x.Id == buildingId)
            .Select(x => new { x.Id, x.Name, x.BuildingNumber })
            .FirstOrDefaultAsync(cancellationToken);
        if (building is null) return null;
        var count = await db.Apartments.AsNoTracking()
            .CountAsync(x => x.BuildingId == buildingId && !x.IsClosed, cancellationToken);
        return new DvrBuildingSummary
        {
            Id = building.Id,
            Name = building.Name,
            BuildingNumber = building.BuildingNumber,
            ApartmentsCount = count
        };
    }

    public async Task<List<DvrBuildingSummary>> GetBuildingSummariesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync(cancellationToken);
        var buildings = await db.Buildings.AsNoTracking()
            .OrderBy(x => x.BuildingNumber)
            .Select(x => new DvrBuildingSummary
            {
                Id = x.Id,
                Name = x.Name,
                BuildingNumber = x.BuildingNumber,
                ApartmentsCount = x.Apartments.Count(a => !a.IsClosed)
            })
            .ToListAsync(cancellationToken);
        return buildings;
    }

    /// <summary>
    /// Gets the by building.
    /// </summary>
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

    /// <summary>
    /// Gets the by ID.
    /// </summary>
    public async Task<DvrEntity?> GetByIdAsync(string id)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        return await db.Dvrs
            .Include(d => d.Cameras)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    /// <summary>
    /// Creates the operation on the DVR.
    /// </summary>
    public async Task<string> CreateAsync(
        string buildingId, string name, string ip, int port,
        string brand, string username, string password)
    {
        var id = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        name = string.IsNullOrWhiteSpace(name) ? "DVR" : name.Trim();
        ip = ip?.Trim() ?? string.Empty;
        if (!DvrNetworkPolicy.IsAllowedEndpoint(ip, port))
            throw new ArgumentException("DVR host must be a private-network IP address and port must be valid.");
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

    /// <summary>
    /// Updates the operation on the DVR.
    /// </summary>
    public async Task UpdateAsync(
        string id, string name, string ip, int port,
        string brand, string username, string? newPassword, bool isActive)
    {
        name = string.IsNullOrWhiteSpace(name) ? "DVR" : name.Trim();
        ip = ip?.Trim() ?? string.Empty;
        if (!DvrNetworkPolicy.IsAllowedEndpoint(ip, port))
            throw new ArgumentException("DVR host must be a private-network IP address and port must be valid.");
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

    /// <summary>
    /// Deletes the operation on the DVR.
    /// </summary>
    public async Task DeleteAsync(string id)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        await db.Dvrs.Where(d => d.Id == id).ExecuteDeleteAsync();
    }

    /// <summary>
    /// Adds the camera.
    /// </summary>
    public async Task<string> AddCameraAsync(string dvrId, string name, int channel, string rtspPath)
    {
        name = string.IsNullOrWhiteSpace(name) ? $"Camera {channel}" : name.Trim();
        rtspPath = rtspPath?.Trim() ?? "";
        if (channel is < 1 or > 256 || !DvrNetworkPolicy.IsSafeRelativeRtspPath(rtspPath))
            throw new ArgumentException("Camera channel or RTSP path is invalid.");

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

    /// <summary>
    /// Deletes the camera.
    /// </summary>
    public async Task<bool> DeleteCameraAsync(string cameraId, string dvrId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var affected = await db.Cameras.Where(c => c.Id == cameraId && c.DvrId == dvrId).ExecuteDeleteAsync();
        return affected == 1;
    }

    /// <summary>
    /// Gets the decrypted password.
    /// </summary>
    public async Task<string> GetDecryptedPasswordAsync(string dvrId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var dvr = await db.Dvrs.FirstOrDefaultAsync(d => d.Id == dvrId)
            ?? throw new KeyNotFoundException();
        return _enc.Decrypt(dvr.PasswordEncrypted);
    }

    /// <summary>
    /// Synchronizes the all to SQL.
    /// </summary>
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

/// <summary>
/// Result of synchronizing DVR devices and their cameras with the media server.
/// </summary>
public class SyncDvrReport
{
    /// <summary>
    /// Gets or sets the dvrs added.
    /// </summary>
    public int DvrsAdded { get; set; }
    /// <summary>
    /// Gets or sets the dvrs updated.
    /// </summary>
    public int DvrsUpdated { get; set; }
    /// <summary>
    /// Gets or sets the cameras added.
    /// </summary>
    public int CamerasAdded { get; set; }
    /// <summary>
    /// Gets or sets the cameras updated.
    /// </summary>
    public int CamerasUpdated { get; set; }
    /// <summary>
    /// Gets or sets the elapsed.
    /// </summary>
    public TimeSpan Elapsed { get; set; }
    /// <summary>
    /// Gets or sets the errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// Gets the dvrs total.
    /// </summary>
    public int DvrsTotal => DvrsAdded + DvrsUpdated;
    /// <summary>
    /// Gets the cameras total.
    /// </summary>
    public int CamerasTotal => CamerasAdded + CamerasUpdated;
}