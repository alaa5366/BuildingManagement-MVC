using System.Text;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Media device inventory and credential references backed by the canonical media tables.</summary>
public sealed class DvrService
{
    private const string SecretProvider = "media-dvr";
    private const string SecretName = "Password";
    private const string SecretKeyVersion = "application-secret-v1";
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IEncryptionService _enc;
    private readonly ILogger<DvrService> _log;

    public DvrService(IDbContextFactory<AppDbContext> sqlFactory, IEncryptionService enc, ILogger<DvrService> log)
    {
        _sqlFactory = sqlFactory;
        _enc = enc;
        _log = log;
    }

    /// <summary> Gets or previews data through GetBuildingSummaryAsync. </summary>
    public async Task<DvrBuildingSummary?> GetBuildingSummaryAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync(cancellationToken);
        var building = await db.Buildings.AsNoTracking()
            .Where(item => item.Id == buildingId)
            .Select(item => new { item.Id, item.Name, item.BuildingNumber })
            .FirstOrDefaultAsync(cancellationToken);
        if (building is null) return null;
        var count = await db.Apartments.AsNoTracking()
            .CountAsync(item => item.BuildingId == buildingId && !item.IsClosed, cancellationToken);
        return new DvrBuildingSummary
        {
            Id = building.Id, Name = building.Name, BuildingNumber = building.BuildingNumber,
            ApartmentsCount = count
        };
    }

    /// <summary> Gets or previews data through GetBuildingSummariesAsync. </summary>
    public async Task<List<DvrBuildingSummary>> GetBuildingSummariesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync(cancellationToken);
        return await db.Buildings.AsNoTracking().OrderBy(item => item.BuildingNumber)
            .Select(item => new DvrBuildingSummary
            {
                Id = item.Id, Name = item.Name, BuildingNumber = item.BuildingNumber,
                ApartmentsCount = item.Apartments.Count(apartment => !apartment.IsClosed)
            }).ToListAsync(cancellationToken);
    }

    /// <summary> Gets or previews data through GetByBuildingAsync. </summary>
    public async Task<List<DvrEntity>> GetByBuildingAsync(string buildingId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var devices = await db.MediaDevices.AsNoTracking().Where(item => item.BuildingId == buildingId)
            .OrderBy(item => item.Name).ToListAsync();
        var ids = devices.Select(item => item.MediaDeviceId).ToList();
        var channels = await db.MediaDeviceChannels.AsNoTracking()
            .Where(item => ids.Contains(item.MediaDeviceId)).OrderBy(item => item.ChannelNumber).ToListAsync();
        return devices.Select(device => Map(device, channels.Where(channel =>
            channel.MediaDeviceId == device.MediaDeviceId).ToList())).ToList();
    }

    /// <summary> Gets or previews data through GetByIdAsync. </summary>
    public async Task<DvrEntity?> GetByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var deviceId)) return null;
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var device = await db.MediaDevices.AsNoTracking()
            .SingleOrDefaultAsync(item => item.MediaDeviceId == deviceId);
        if (device is null) return null;
        var channels = await db.MediaDeviceChannels.AsNoTracking()
            .Where(item => item.MediaDeviceId == deviceId).OrderBy(item => item.ChannelNumber).ToListAsync();
        return Map(device, channels);
    }

    /// <summary> Gets or previews data through GetCameraAsync. </summary>
    public async Task<(CameraEntity Camera, DvrEntity Device)?> GetCameraAsync(string cameraId)
    {
        if (!Guid.TryParse(cameraId, out var channelId)) return null;
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var channel = await db.MediaDeviceChannels.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ChannelId == channelId);
        if (channel is null) return null;
        var device = await db.MediaDevices.AsNoTracking()
            .SingleOrDefaultAsync(item => item.MediaDeviceId == channel.MediaDeviceId);
        if (device is null) return null;
        var camera = new CameraEntity
        {
            Id = channel.ChannelId.ToString("N"),
            DvrId = device.MediaDeviceId.ToString("N"),
            BuildingId = channel.BuildingId,
            Name = channel.Name,
            Channel = channel.ChannelNumber,
            RtspPath = channel.RtspPath,
            IsActive = channel.IsActive,
            CreatedAt = channel.CreatedAtUtc
        };
        return (camera, Map(device, new List<MediaDeviceChannelEntity> { channel }));
    }

    /// <summary> Creates or saves data through CreateAsync. </summary>
    public async Task<string> CreateAsync(
        string buildingId, string name, string ip, int port,
        string brand, string username, string password)
    {
        name = string.IsNullOrWhiteSpace(name) ? "DVR" : name.Trim();
        ip = ip?.Trim() ?? "";
        if (!DvrNetworkPolicy.IsAllowedEndpoint(ip, port))
            throw new ArgumentException("DVR host must be a private-network IP address and port must be valid.");
        brand = string.IsNullOrWhiteSpace(brand) ? "Other" : brand.Trim();
        username = username?.Trim() ?? "";
        password ??= "";

        var now = DateTime.UtcNow;
        var deviceId = Guid.NewGuid();
        var deviceReference = deviceId.ToString("N");
        await using var db = await _sqlFactory.CreateDbContextAsync();
        if (!await db.Buildings.AnyAsync(item => item.Id == buildingId))
            throw new InvalidOperationException("building-not-found");
        if (await db.MediaDevices.AnyAsync(item => item.BuildingId == buildingId
            && item.HostAddress == ip && item.Port == port))
            throw new InvalidOperationException("media-device-already-exists");

        db.MediaDevices.Add(new MediaDeviceEntity
        {
            MediaDeviceId = deviceId, BuildingId = buildingId, Name = name,
            HostAddress = ip, Port = port, Brand = brand, Username = username,
            IsActive = true, CreatedAtUtc = now
        });
        var secretId = Guid.NewGuid();
        db.ProtectedIntegrationSecrets.Add(new ProtectedIntegrationSecretEntity
        {
            SecretId = secretId, ScopeType = 1, ScopeReference = deviceReference,
            Provider = SecretProvider, SecretName = SecretName,
            Ciphertext = Encoding.UTF8.GetBytes(_enc.Encrypt(password)),
            KeyVersion = SecretKeyVersion, CreatedAtUtc = now
        });
        db.MediaCredentialReferences.Add(new MediaCredentialReferenceEntity
        {
            CredentialReferenceId = Guid.NewGuid(), BuildingId = buildingId,
            DvrLegacyId = deviceReference, VaultProvider = "ProtectedIntegrationSecrets",
            VaultSecretReference = secretId.ToString("D"), KeyVersion = SecretKeyVersion, CreatedAtUtc = now
        });
        db.MediaProvisioningOperations.Add(new MediaProvisioningOperationEntity
        {
            OperationId = Guid.NewGuid(), DvrLegacyId = deviceReference,
            OperationType = 1, Status = 0, RequestedAtUtc = now, Attempts = 0
        });
        await db.SaveChangesAsync();
        return deviceReference;
    }

    /// <summary> Updates account or operational data through UpdateAsync. </summary>
    public async Task UpdateAsync(
        string id, string name, string ip, int port,
        string brand, string username, string? newPassword, bool isActive)
    {
        if (!Guid.TryParse(id, out var deviceId)) throw new KeyNotFoundException($"DVR {id} not found");
        name = string.IsNullOrWhiteSpace(name) ? "DVR" : name.Trim();
        ip = ip?.Trim() ?? "";
        if (!DvrNetworkPolicy.IsAllowedEndpoint(ip, port))
            throw new ArgumentException("DVR host must be a private-network IP address and port must be valid.");
        brand = string.IsNullOrWhiteSpace(brand) ? "Other" : brand.Trim();
        username = username?.Trim() ?? "";
        var now = DateTime.UtcNow;

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var device = await db.MediaDevices.SingleOrDefaultAsync(item => item.MediaDeviceId == deviceId)
            ?? throw new KeyNotFoundException($"DVR {id} not found");
        device.Name = name;
        device.HostAddress = ip;
        device.Port = port;
        device.Brand = brand;
        device.Username = username;
        device.IsActive = isActive;
        device.UpdatedAtUtc = now;

        if (!string.IsNullOrEmpty(newPassword))
            await RotateCredentialAsync(db, id, newPassword, now);
        await db.SaveChangesAsync();
    }

    /// <summary> Removes or revokes data through DeleteAsync. </summary>
    public async Task DeleteAsync(string id)
    {
        if (!Guid.TryParse(id, out var deviceId)) return;
        var now = DateTime.UtcNow;
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var device = await db.MediaDevices.SingleOrDefaultAsync(item => item.MediaDeviceId == deviceId);
        if (device is null) return;
        device.IsActive = false;
        device.UpdatedAtUtc = now;
        var reference = await db.MediaCredentialReferences.SingleOrDefaultAsync(item => item.DvrLegacyId == id);
        if (reference is not null) reference.RevokedAtUtc = now;
        if (reference is not null && Guid.TryParse(reference.VaultSecretReference, out var secretId))
        {
            var secret = await db.ProtectedIntegrationSecrets.SingleOrDefaultAsync(item => item.SecretId == secretId);
            if (secret is not null) secret.RevokedAtUtc = now;
        }
        var channelIds = await db.MediaDeviceChannels.Where(item => item.MediaDeviceId == deviceId)
            .Select(item => item.ChannelId.ToString("N")).ToListAsync();
        var sessions = await db.MediaAccessSessions.Where(item => item.CameraLegacyId != null
            && channelIds.Contains(item.CameraLegacyId) && item.RevokedAtUtc == null).ToListAsync();
        foreach (var session in sessions) session.RevokedAtUtc = now;
        var channels = await db.MediaDeviceChannels.Where(item => item.MediaDeviceId == deviceId).ToListAsync();
        foreach (var channel in channels) channel.IsActive = false;
        db.MediaProvisioningOperations.Add(new MediaProvisioningOperationEntity
        {
            OperationId = Guid.NewGuid(), DvrLegacyId = id, OperationType = 2,
            Status = 0, RequestedAtUtc = now, Attempts = 0
        });
        await db.SaveChangesAsync();
    }

    /// <summary> Creates or saves data through AddCameraAsync. </summary>
    public async Task<string> AddCameraAsync(string dvrId, string name, int channel, string rtspPath)
    {
        name = string.IsNullOrWhiteSpace(name) ? $"Camera {channel}" : name.Trim();
        rtspPath = rtspPath?.Trim() ?? "";
        if (channel is < 1 or > 256 || !DvrNetworkPolicy.IsSafeRelativeRtspPath(rtspPath))
            throw new ArgumentException("Camera channel or RTSP path is invalid.");
        if (!Guid.TryParse(dvrId, out var deviceId)) throw new KeyNotFoundException($"DVR {dvrId} not found");

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var device = await db.MediaDevices.SingleOrDefaultAsync(item =>
            item.MediaDeviceId == deviceId && item.IsActive)
            ?? throw new KeyNotFoundException($"DVR {dvrId} not found");
        var now = DateTime.UtcNow;
        var channelId = Guid.NewGuid();
        db.MediaDeviceChannels.Add(new MediaDeviceChannelEntity
        {
            ChannelId = channelId, MediaDeviceId = deviceId, BuildingId = device.BuildingId,
            Name = name, ChannelNumber = channel, RtspPath = rtspPath,
            IsActive = true, CreatedAtUtc = now
        });
        await db.SaveChangesAsync();
        return channelId.ToString("N");
    }

    /// <summary> Removes or revokes data through DeleteCameraAsync. </summary>
    public async Task<bool> DeleteCameraAsync(string cameraId, string dvrId)
    {
        if (!Guid.TryParse(cameraId, out var channelId) || !Guid.TryParse(dvrId, out var deviceId)) return false;
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var channel = await db.MediaDeviceChannels.SingleOrDefaultAsync(item =>
            item.ChannelId == channelId && item.MediaDeviceId == deviceId);
        if (channel is null) return false;
        channel.IsActive = false;
        channel.UpdatedAtUtc = DateTime.UtcNow;
        var reference = channelId.ToString("N");
        var sessions = await db.MediaAccessSessions.Where(item =>
            item.CameraLegacyId == reference && item.RevokedAtUtc == null).ToListAsync();
        foreach (var session in sessions) session.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary> Gets or previews data through GetDecryptedPasswordAsync. </summary>
    public async Task<string> GetDecryptedPasswordAsync(string dvrId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var reference = await db.MediaCredentialReferences.AsNoTracking()
            .SingleOrDefaultAsync(item => item.DvrLegacyId == dvrId && item.RevokedAtUtc == null)
            ?? throw new KeyNotFoundException();
        if (!Guid.TryParse(reference.VaultSecretReference, out var secretId))
            throw new InvalidOperationException("Media credential reference is invalid.");
        var secret = await db.ProtectedIntegrationSecrets.AsNoTracking()
            .SingleOrDefaultAsync(item => item.SecretId == secretId && item.RevokedAtUtc == null)
            ?? throw new KeyNotFoundException();
        return _enc.Decrypt(Encoding.UTF8.GetString(secret.Ciphertext));
    }

    /// <summary> Updates account or operational data through SyncAllToSqlAsync. </summary>
    public Task<SyncDvrReport> SyncAllToSqlAsync() =>
        Task.FromResult(new SyncDvrReport { Elapsed = TimeSpan.Zero });

    private async Task RotateCredentialAsync(AppDbContext db, string deviceReference, string password, DateTime now)
    {
        var reference = await db.MediaCredentialReferences.SingleOrDefaultAsync(item =>
            item.DvrLegacyId == deviceReference && item.RevokedAtUtc == null)
            ?? throw new InvalidOperationException("Media credential reference is missing.");
        if (!Guid.TryParse(reference.VaultSecretReference, out var secretId))
            throw new InvalidOperationException("Media credential reference is invalid.");
        var secret = await db.ProtectedIntegrationSecrets.SingleOrDefaultAsync(item => item.SecretId == secretId)
            ?? throw new InvalidOperationException("Media secret is missing.");
        secret.Ciphertext = Encoding.UTF8.GetBytes(_enc.Encrypt(password));
        secret.KeyVersion = SecretKeyVersion;
        secret.RotatedAtUtc = now;
        reference.RotatedAtUtc = now;
    }

    private static DvrEntity Map(MediaDeviceEntity device, List<MediaDeviceChannelEntity> channels) => new()
    {
        Id = device.MediaDeviceId.ToString("N"),
        BuildingId = device.BuildingId,
        Name = device.Name,
        IpAddress = device.HostAddress,
        Port = device.Port,
        Brand = device.Brand,
        Username = device.Username,
        PasswordEncrypted = "",
        IsActive = device.IsActive,
        CreatedAt = device.CreatedAtUtc,
        UpdatedAt = device.UpdatedAtUtc,
        Cameras = channels.Select(channel => new CameraEntity
        {
            Id = channel.ChannelId.ToString("N"),
            DvrId = device.MediaDeviceId.ToString("N"),
            BuildingId = channel.BuildingId,
            Name = channel.Name,
            Channel = channel.ChannelNumber,
            RtspPath = channel.RtspPath,
            IsActive = channel.IsActive,
            CreatedAt = channel.CreatedAtUtc
        }).ToList()
    };
}

    /// <summary> Represents the SyncDvrReport data or service contract. </summary>
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
