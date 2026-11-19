using System.Security.Cryptography;
using System.Text;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Stores stable QR tokens as hashed, revocable target-schema capabilities.</summary>
public sealed class QrTokenStoreService
{
    private const byte StableApartmentTarget = 1;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<QrTokenStoreService> _logger;

    public QrTokenStoreService(IDbContextFactory<AppDbContext> factory, ILogger<QrTokenStoreService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    /// <summary> Creates or saves data through SaveTokenAsync. </summary>
    public async Task<bool> SaveTokenAsync(AccessTokenPayload payload)
    {
        if (payload is null || string.IsNullOrWhiteSpace(payload.Uid) || payload.Uid.Length > 128 ||
            string.IsNullOrWhiteSpace(payload.Bld) || string.IsNullOrWhiteSpace(payload.Apt))
            return false;
        try
        {
            var issuedAt = DateTimeOffset.FromUnixTimeMilliseconds(payload.Ts).UtcDateTime;
            var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(payload.Exp).UtcDateTime;
            if (expiresAt <= issuedAt) return false;
            await using var db = await _factory.CreateDbContextAsync();
            var capabilityId = StableCapabilityId(payload.Uid);
            if (await db.QrCapabilities.AnyAsync(item => item.CapabilityId == capabilityId))
                return false;
            db.QrCapabilities.Add(new QrCapabilityEntity
            {
                CapabilityId = capabilityId,
                BuildingId = payload.Bld,
                ApartmentId = payload.Apt,
                TargetType = StableApartmentTarget,
                TargetReference = payload.Apt,
                TokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(payload.Uid)),
                IssuedAtUtc = issuedAt,
                ExpiresAtUtc = expiresAt,
                MaxUses = string.Equals(payload.Use, "single", StringComparison.OrdinalIgnoreCase)
                    ? 1 : int.MaxValue
            });
            await db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to persist stable QR capability");
            return false;
        }
    }

    /// <summary> Gets or previews data through PreviewTargetAsync. </summary>
    public Task<(bool Allowed, string? Reason, string? Bld, string? Apt)> PreviewTargetAsync(
        string uid, CancellationToken cancellationToken = default) =>
        ValidateAsync(uid, consume: false, cancellationToken);

    /// <summary> Performs the CheckAndUseTokenAsync operation. </summary>
    public Task<(bool Allowed, string? Reason, string? Bld, string? Apt)> CheckAndUseTokenAsync(string uid) =>
        ValidateAsync(uid, consume: true, CancellationToken.None);

    private async Task<(bool Allowed, string? Reason, string? Bld, string? Apt)> ValidateAsync(
        string uid, bool consume, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(uid) || uid.Length > 128)
            return (false, "not-found", null, null);
        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            var capabilityId = StableCapabilityId(uid);
            var now = DateTime.UtcNow;
            await using var transaction = consume && db.Database.IsRelational()
                ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
                : null;
            var capability = await db.QrCapabilities.SingleOrDefaultAsync(item =>
                item.CapabilityId == capabilityId && item.TargetType == StableApartmentTarget, cancellationToken);
            if (capability is null ||
                !CryptographicOperations.FixedTimeEquals(
                    capability.TokenHash, SHA256.HashData(Encoding.UTF8.GetBytes(uid))))
                return (false, "not-found", null, null);
            if (capability.RevokedAtUtc.HasValue) return (false, "revoked", null, null);
            if (now > capability.ExpiresAtUtc) return (false, "expired", null, null);

            var used = await db.QrCapabilityUses.CountAsync(item =>
                item.CapabilityId == capability.CapabilityId && item.Outcome == 1, cancellationToken);
            if (used >= capability.MaxUses) return (false, "already-used", null, null);

            if (consume)
            {
                db.QrCapabilityUses.Add(new QrCapabilityUseEntity
                {
                    UseId = Guid.NewGuid(), CapabilityId = capability.CapabilityId,
                    UsedAtUtc = now, UseReference = "stable-qr", Outcome = 1
                });
                await db.SaveChangesAsync(cancellationToken);
                if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            }
            return (true, null, capability.BuildingId, capability.ApartmentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to validate stable QR capability");
            return (false, "error", null, null);
        }
    }

    /// <summary> Removes or revokes data through DeleteTokenAsync. </summary>
    public async Task DeleteTokenAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid) || uid.Length > 128) return;
        await using var db = await _factory.CreateDbContextAsync();
        var capability = await db.QrCapabilities.SingleOrDefaultAsync(item =>
            item.CapabilityId == StableCapabilityId(uid) && item.TargetType == StableApartmentTarget);
        if (capability is null || capability.RevokedAtUtc.HasValue) return;
        capability.RevokedAtUtc = DateTime.UtcNow;
        capability.RevokedByReference = "system";
        await db.SaveChangesAsync();
    }

    private static Guid StableCapabilityId(string uid)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(uid));
        return new Guid(hash.AsSpan(0, 16));
    }
}
