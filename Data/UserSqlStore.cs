// =====================================================================
//  UserSqlStore - مرآة SQL لمستندات users (الأدمنز والسوبر أدمن)
//  + قراءة كاملة لدعم وضع Sql
// =====================================================================
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

public sealed class UserSqlStore
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<UserSqlStore> _log;

    public UserSqlStore(IDbContextFactory<AppDbContext> factory, ILogger<UserSqlStore> log)
    {
        _factory = factory;
        _log = log;
    }

    // =====================================================================
    //  Upsert (كتابة)
    // =====================================================================
    public async Task<bool> UpsertAsync(AppUserDoc u, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(u.Uid)) return false;
        if (u.Role is not ("superadmin" or "admin" or "resident"))
        {
            _log.LogWarning("User {Uid} role '{Role}' غير معروف - اتخطّى", u.Uid, u.Role);
            return false;
        }

        using var _ = await KeyedLock.AcquireAsync("u:" + u.Uid, ct);
        await using var db = await _factory.CreateDbContextAsync(ct);

        var uid = BuildingSqlMapper.Cut(u.Uid, 128);
        var row = await db.Users.FirstOrDefaultAsync(x => x.Uid == uid, ct);
        if (row == null)
        {
            row = new UserEntity { Uid = uid };
            db.Users.Add(row);
        }

        row.Email = BuildingSqlMapper.Cut(u.Email, 256);
        row.Name = BuildingSqlMapper.Cut(u.Name, 200);
        row.Phone = BuildingSqlMapper.Cut(u.Phone, 32);
        row.Whatsapp = BuildingSqlMapper.Cut(u.Whatsapp, 32);
        row.Pin = BuildingSqlMapper.Cut(u.Pin, 256);
        row.PhotoUrl = BuildingSqlMapper.Cut(u.PhotoUrl, 1000);
        row.Role = u.Role;
        row.IsDisabled = u.Disabled;
        row.DisabledReason = BuildingSqlMapper.Cut(u.DisabledReason, 500);
        row.IsActive = u.IsActive;
        row.LastLoginAt = BuildingSqlMapper.ParseTs(u.LastLoginAt);
        row.CreatedAt = BuildingSqlMapper.ParseTs(u.CreatedAt);
        row.CreatedBy = u.CreatedBy == null ? null : BuildingSqlMapper.Cut(u.CreatedBy, 128);
        row.UpdatedAt = BuildingSqlMapper.ParseTs(u.UpdatedAt);
        row.UpdatedBy = u.UpdatedBy == null ? null : BuildingSqlMapper.Cut(u.UpdatedBy, 128);

        var want = (u.Permissions ?? new()).Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => BuildingSqlMapper.Cut(p, 60)).ToHashSet();
        var have = await db.UserPermissions.Where(x => x.Uid == uid).ToListAsync(ct);
        db.UserPermissions.RemoveRange(have.Where(h => !want.Contains(h.Permission)));
        foreach (var p in want.Except(have.Select(h => h.Permission)))
            db.UserPermissions.Add(new UserPermissionEntity { Uid = uid, Permission = p });

        if (u.Role == "admin" && u.BuildingIds is { Count: > 0 })
        {
            var ids = u.BuildingIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            var inSql = await db.Buildings.Where(b => ids.Contains(b.Id)).Select(b => b.Id).ToListAsync(ct);
            var linked = await db.BuildingAdmins.Where(x => x.AdminUid == uid)
                .Select(x => x.BuildingId).ToListAsync(ct);
            foreach (var bid in inSql.Except(linked))
                db.BuildingAdmins.Add(new BuildingAdminEntity { BuildingId = bid, AdminUid = uid });
        }

        await db.SaveChangesAsync(ct);
        SqlMirrorHealth.RecordSuccess();
        return true;
    }

    // =====================================================================
    //  قراءة واحدة
    // =====================================================================
    public async Task<UserEntity?> GetByUidAsync(string uid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(uid)) return null;
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Users
            .Include(u => u.Permissions)
            .Include(u => u.BuildingAdmins)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Uid == uid, ct);
    }

    // =====================================================================
    //  قراءة الكل
    // =====================================================================
    public async Task<List<UserEntity>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Users
            .Include(u => u.Permissions)
            .Include(u => u.BuildingAdmins)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // =====================================================================
    //  قراءة الأدمنز
    // =====================================================================
    public async Task<List<UserEntity>> GetAdminsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Users
            .Include(u => u.Permissions)
            .Include(u => u.BuildingAdmins)
            .AsNoTracking()
            .Where(u => u.Role == "admin")
            .ToListAsync(ct);
    }

    // =====================================================================
    //  قراءة أدمنز مبنى معين
    // =====================================================================
    public async Task<List<UserEntity>> GetBuildingAdminsAsync(string buildingId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId)) return new();
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Users
            .Include(u => u.Permissions)
            .Include(u => u.BuildingAdmins)
            .AsNoTracking()
            .Where(u => u.BuildingAdmins.Any(ba => ba.BuildingId == buildingId))
            .ToListAsync(ct);
    }
}