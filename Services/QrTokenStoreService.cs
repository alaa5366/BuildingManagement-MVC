using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Stores and looks up stable QR access tokens.
/// </summary>
public class QrTokenStoreService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<QrTokenStoreService> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="QrTokenStoreService"/> class.
    /// </summary>
    public QrTokenStoreService(IDbContextFactory<AppDbContext> sqlFactory, ILogger<QrTokenStoreService> log)
    {
        _sqlFactory = sqlFactory;
        _log = log;
    }

    /// <summary>
    /// Saves the token.
    /// </summary>
    public async Task<bool> SaveTokenAsync(AccessTokenPayload payload)
    {
        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();

            db.QrTokens.Add(new QrTokenEntity
            {
                Uid = payload.Uid,
                BuildingId = payload.Bld,
                ApartmentId = payload.Apt,
                ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(payload.Exp).UtcDateTime,
                UseType = payload.Use,
                IssuedAt = DateTimeOffset.FromUnixTimeMilliseconds(payload.Ts).UtcDateTime,
                CreatedBy = payload.By,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "SaveTokenAsync failed");
            return false;
        }
    }

    /// <summary>
    /// Checks the and use token.
    /// </summary>
    /// <summary>
    /// Reads the signed token target without consuming it. The caller must still
    /// atomically consume it with CheckAndUseTokenAsync after validating the target.
    /// </summary>
    public async Task<(bool Allowed, string? Reason, string? Bld, string? Apt)> PreviewTargetAsync(
        string uid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uid) || uid.Length > 128)
            return (false, "not-found", null, null);

        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync(cancellationToken);
            var token = await db.QrTokens.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Uid == uid, cancellationToken);
            if (token is null) return (false, "not-found", null, null);
            if (DateTime.UtcNow > token.ExpiresAt) return (false, "expired", null, null);
            if (token.UseType == "single" && token.IsUsed) return (false, "already-used", null, null);
            return (true, null, token.BuildingId, token.ApartmentId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "PreviewTargetAsync failed");
            return (false, "error", null, null);
        }
    }

    public async Task<(bool Allowed, string? Reason, string? Bld, string? Apt)> CheckAndUseTokenAsync(string uid)
    {
        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var token = await db.QrTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Uid == uid);
            if (token == null) return (false, "not-found", null, null);
            if (DateTime.UtcNow > token.ExpiresAt) return (false, "expired", null, null);
            if (token.UseType == "single")
            {
                // Atomic compare-and-set: only one concurrent request can consume a single-use token.
                var affected = await db.QrTokens
                    .Where(t => t.Uid == uid && !t.IsUsed && t.ExpiresAt >= DateTime.UtcNow)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(t => t.IsUsed, true)
                        .SetProperty(t => t.UsedAt, DateTime.UtcNow));
                if (affected != 1) return (false, "already-used", null, null);
            }
            else
            {
                var affected = await db.QrTokens.Where(t => t.Uid == uid && t.ExpiresAt >= DateTime.UtcNow)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UsedAt, DateTime.UtcNow));
                if (affected != 1) return (false, "expired", null, null);
            }
            return (true, null, token.BuildingId, token.ApartmentId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "CheckAndUseTokenAsync failed");
            return (false, "error", null, null);
        }
    }

    /// <summary>
    /// Deletes the token.
    /// </summary>
    public async Task DeleteTokenAsync(string uid)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        await db.QrTokens.Where(t => t.Uid == uid).ExecuteDeleteAsync();
    }
}