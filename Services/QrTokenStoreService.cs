using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class QrTokenStoreService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<QrTokenStoreService> _log;

    public QrTokenStoreService(IDbContextFactory<AppDbContext> sqlFactory, ILogger<QrTokenStoreService> log)
    {
        _sqlFactory = sqlFactory;
        _log = log;
    }

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

    public async Task<(bool Allowed, string? Reason, string? Bld, string? Apt)> CheckAndUseTokenAsync(string uid)
    {
        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var token = await db.QrTokens.FirstOrDefaultAsync(t => t.Uid == uid);

            if (token == null) return (false, "not-found", null, null);

            if (DateTime.UtcNow > token.ExpiresAt)
                return (false, "expired", null, null);

            if (token.UseType == "single" && token.IsUsed)
                return (false, "already-used", null, null);

            token.IsUsed = true;
            token.UsedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return (true, null, token.BuildingId, token.ApartmentId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "CheckAndUseTokenAsync failed");
            return (false, "error", null, null);
        }
    }

    public async Task DeleteTokenAsync(string uid)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        await db.QrTokens.Where(t => t.Uid == uid).ExecuteDeleteAsync();
    }
}