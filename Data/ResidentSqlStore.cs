// =====================================================================
//  ResidentSqlStore — SQL operations للساكنين
//  بيستخدم IDbContextFactory (Singleton-safe)
// =====================================================================
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

public sealed class ResidentSqlStore
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<ResidentSqlStore> _log;

    public ResidentSqlStore(
        IDbContextFactory<AppDbContext> factory,
        ILogger<ResidentSqlStore> log)
    {
        _factory = factory;
        _log = log;
    }

    // ═══════════════════════════════════════════════════════════
    // 1. القراءة
    // ═══════════════════════════════════════════════════════════

    public async Task<ResidentEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<ResidentEntity?> GetByUidAsync(string uid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(uid)) return null;
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Uid == uid, ct);
    }

    public async Task<List<ResidentEntity>> GetByApartmentAsync(
        string buildingId, string apartmentId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Residents
            .AsNoTracking()
            .Where(r => r.BuildingId == buildingId && r.ApartmentId == apartmentId)
            .OrderByDescending(r => r.IsPrimary)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task<List<ResidentEntity>> GetByBuildingAsync(
        string buildingId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Residents
            .AsNoTracking()
            .Where(r => r.BuildingId == buildingId)
            .OrderBy(r => r.ApartmentId)
            .ThenByDescending(r => r.IsPrimary)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task<List<ResidentEntity>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Residents
            .AsNoTracking()
            .OrderBy(r => r.BuildingId)
            .ThenBy(r => r.ApartmentId)
            .ToListAsync(ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 2. البحث
    // ═══════════════════════════════════════════════════════════

    public async Task<ResidentEntity?> FindByPhoneAsync(
        string buildingId, string phone, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(phone))
            return null;

        var normalized = AuthHelpers.NormalizePhone(phone);
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Residents
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.BuildingId == buildingId && r.Phone == normalized, ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 3. Upsert
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> UpsertAsync(ResidentEntity resident, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(resident.Id)) return false;

        using var _ = await KeyedLock.AcquireAsync("res:" + resident.Id, ct);
        await using var db = await _factory.CreateDbContextAsync(ct);

        var row = await db.Residents.FirstOrDefaultAsync(r => r.Id == resident.Id, ct);
        if (row == null)
        {
            row = new ResidentEntity { Id = resident.Id };
            db.Residents.Add(row);
        }

        row.Uid = BuildingSqlMapper.Cut(resident.Uid, 128);
        row.BuildingId = BuildingSqlMapper.Cut(resident.BuildingId, 64);
        row.ApartmentId = BuildingSqlMapper.Cut(resident.ApartmentId, 64);
        row.Name = BuildingSqlMapper.Cut(resident.Name, 200);
        row.Phone = BuildingSqlMapper.Cut(resident.Phone, 32);
        row.Whatsapp = BuildingSqlMapper.Cut(resident.Whatsapp, 32);
        row.Email = BuildingSqlMapper.Cut(resident.Email, 256);
        row.PhotoUrl = BuildingSqlMapper.Cut(resident.PhotoUrl, 1000);
        row.PinHash = BuildingSqlMapper.Cut(resident.PinHash, 256);
        row.PasswordHash = resident.PasswordHash;
        row.IsActive = resident.IsActive;
        row.IsDisabled = resident.IsDisabled;
        row.DisabledReason = BuildingSqlMapper.Cut(resident.DisabledReason, 500);
        row.IsOwner = resident.IsOwner;
        row.IsPrimary = resident.IsPrimary;
        row.LastLoginAt = resident.LastLoginAt;
        row.CreatedAt = resident.CreatedAt;
        row.CreatedBy = resident.CreatedBy;
        row.UpdatedAt = resident.UpdatedAt;
        row.UpdatedBy = resident.UpdatedBy;

        await db.SaveChangesAsync(ct);
        return true;
    }

    // ═══════════════════════════════════════════════════════════
    // 4. Bulk Upsert
    // ═══════════════════════════════════════════════════════════

    public async Task<int> UpsertManyAsync(
        IEnumerable<ResidentEntity> residents, CancellationToken ct = default)
    {
        var count = 0;
        foreach (var r in residents)
        {
            if (await UpsertAsync(r, ct)) count++;
        }
        return count;
    }

    // ═══════════════════════════════════════════════════════════
    // 5. حذف
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        await using var db = await _factory.CreateDbContextAsync(ct);
        var affected = await db.Residents.Where(r => r.Id == id).ExecuteDeleteAsync(ct);
        return affected > 0;
    }

    // ═══════════════════════════════════════════════════════════
    // 6. Update Last Login
    // ═══════════════════════════════════════════════════════════

    public async Task UpdateLastLoginAsync(string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Residents
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastLoginAt, DateTime.UtcNow), ct);
    }

    // ═══════════════════════════════════════════════════════════
    // 7. Count
    // ═══════════════════════════════════════════════════════════

    public async Task<int> CountAsync(string? buildingId = null, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var query = db.Residents.AsQueryable();
        if (!string.IsNullOrWhiteSpace(buildingId))
            query = query.Where(r => r.BuildingId == buildingId);
        return await query.CountAsync(ct);
    }
}