using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class ResidentsService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ResidentsService> _logger;

    public ResidentsService(
        IDbContextFactory<AppDbContext> sqlFactory,
        IAuditLogger audit,
        ILogger<ResidentsService> logger)
    {
        _sqlFactory = sqlFactory;
        _audit = audit;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    // ✅ دالة مركزية لتحديث PIN الساكن (تحدث الشقق والسكان معاً)
    // ═══════════════════════════════════════════════════════════
    // تعيين PIN للشقة (رابط التعيين أو إعادة التعيين من الأدمن).
    // لو مفيش ساكن للشقة بنعمل ساكن رئيسي من بيانات الشقة.
    public async Task SetApartmentPinAsync(string buildingId, string apartmentId, string newPin, string? updatedBy)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var hashed = SecretVerifier.Hash(newPin);

        var residents = await db.Residents
            .Where(r => r.BuildingId == buildingId && r.ApartmentId == apartmentId)
            .ToListAsync();

        if (residents.Count == 0)
        {
            var apt = await db.Apartments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.BuildingId == buildingId && a.Id == apartmentId)
                ?? throw new InvalidOperationException("apartment-not-found");

            db.Residents.Add(new BuildingManagementMvc.Data.Entities.ResidentEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                Uid = Guid.NewGuid().ToString("N"),
                BuildingId = buildingId,
                ApartmentId = apartmentId,
                Name = apt.Owner ?? "",
                Phone = apt.Phone ?? "",
                Whatsapp = apt.Phone ?? "",
                Email = apt.Email ?? "",
                PinHash = hashed,
                IsPrimary = true,
                IsOwner = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = updatedBy
            });
        }
        else
        {
            foreach (var res in residents) res.PinHash = hashed;
        }

        await db.SaveChangesAsync();
    }

    public async Task UpdateResidentPinAsync(string buildingId, string apartmentId, string newPin)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();

        // الـ PIN مابقاش بيتخزّن نص صريح في جدول الشقق — بنحدّث جدول السكان (مشفر) بس
        var residents = await db.Residents
            .Where(r => r.ApartmentId == apartmentId)
            .ToListAsync();

        if (residents.Any())
        {
            var hashedPin = BCrypt.Net.BCrypt.HashPassword(newPin);
            foreach (var res in residents)
            {
                res.PinHash = hashedPin ?? string.Empty;
            }
        }

        await db.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // الدوال الأصلية (مع تصحيح أنواع التواريخ والتحذيرات)
    // ═══════════════════════════════════════════════════════════
    public async Task<List<ResidentEntity>> GetByBuildingAsync(string buildingId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        return await db.Residents
            .Where(r => r.BuildingId == buildingId)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<List<ResidentEntity>> GetByApartmentAsync(string buildingId, string apartmentId)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        return await db.Residents
            .Where(r => r.BuildingId == buildingId && r.ApartmentId == apartmentId)
            .ToListAsync();
    }

    public async Task<ResidentEntity?> GetByIdAsync(string id)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        return await db.Residents.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task CreateAsync(
        string buildingId, string apartmentId,
        string name, string phone, string whatsapp, string pin, string? email,
        bool isPrimary, bool isOwner, string createdBy)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var resident = new ResidentEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            Uid = Guid.NewGuid().ToString("N"),
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            Name = name,
            Phone = AuthHelpers.NormalizePhone(phone),
            Whatsapp = AuthHelpers.NormalizePhone(whatsapp),
            PinHash = BCrypt.Net.BCrypt.HashPassword(pin),
            Email = email ?? string.Empty,
            IsPrimary = isPrimary,
            IsOwner = isOwner,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy ?? string.Empty
        };
        db.Residents.Add(resident);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(
        string id, string name, string phone, string whatsapp, string? email,
        bool isPrimary, bool isOwner, string updatedBy)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var resident = await db.Residents.FirstOrDefaultAsync(r => r.Id == id);
        if (resident == null) return;

        resident.Name = name;
        resident.Phone = AuthHelpers.NormalizePhone(phone);
        resident.Whatsapp = AuthHelpers.NormalizePhone(whatsapp);
        resident.Email = email ?? string.Empty;
        resident.IsPrimary = isPrimary;
        resident.IsOwner = isOwner;
        resident.UpdatedAt = DateTime.UtcNow;
        resident.UpdatedBy = updatedBy ?? string.Empty;

        await db.SaveChangesAsync();
    }

    public async Task ChangePinAsync(string id, string newPin, string updatedBy)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var resident = await db.Residents.FirstOrDefaultAsync(r => r.Id == id);
        if (resident == null) return;

        resident.PinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
        resident.UpdatedAt = DateTime.UtcNow;
        resident.UpdatedBy = updatedBy ?? string.Empty;

        await db.SaveChangesAsync();
    }

    public async Task SetDisabledAsync(string id, bool disabled, string? reason, string updatedBy)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var resident = await db.Residents.FirstOrDefaultAsync(r => r.Id == id);
        if (resident == null) return;

        resident.IsDisabled = disabled;
        resident.DisabledReason = disabled ? (reason ?? string.Empty) : string.Empty;
        resident.UpdatedAt = DateTime.UtcNow;
        resident.UpdatedBy = updatedBy ?? string.Empty;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id, string deletedBy)
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var resident = await db.Residents.FirstOrDefaultAsync(r => r.Id == id);
        if (resident == null) return;

        db.Residents.Remove(resident);
        await db.SaveChangesAsync();
    }

    public async Task<int> CountAsync()
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();
        return await db.Residents.CountAsync();
    }

    public async Task<MigrationResult> MigrateFromApartmentsAsync(string? buildingId, bool dryRun)
    {
        return new MigrationResult();
    }
}