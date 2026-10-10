// =====================================================================
//  ResidentsService — Business Logic للساكنين
//  بيشتغل مع:
//    - ResidentSqlStore (SQL)
//    - IPasswordHasher (BCrypt)
//    - BuildingsService (Apartments)
// =====================================================================
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

public class ResidentsService
{
    private readonly ResidentSqlStore _store;
    private readonly BuildingsService _buildings;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ResidentsService> _log;

    public ResidentsService(
        ResidentSqlStore store,
        BuildingsService buildings,
        IPasswordHasher hasher,
        IAuditLogger audit,
        ILogger<ResidentsService> log)
    {
        _store = store;
        _buildings = buildings;
        _hasher = hasher;
        _audit = audit;
        _log = log;
    }

    // ═══════════════════════════════════════════════════════════
    // 1. قراءة
    // ═══════════════════════════════════════════════════════════

    public Task<List<ResidentEntity>> GetByApartmentAsync(string buildingId, string apartmentId)
        => _store.GetByApartmentAsync(buildingId, apartmentId);

    public Task<List<ResidentEntity>> GetByBuildingAsync(string buildingId)
        => _store.GetByBuildingAsync(buildingId);

    public Task<ResidentEntity?> GetByIdAsync(string id)
        => _store.GetByIdAsync(id);

    public Task<int> CountAsync(string? buildingId = null)
        => _store.CountAsync(buildingId);

    // ═══════════════════════════════════════════════════════════
    // 2. إنشاء ساكن جديد
    // ═══════════════════════════════════════════════════════════

    public async Task<ResidentEntity> CreateAsync(
        string buildingId,
        string apartmentId,
        string name,
        string phone,
        string whatsapp,
        string pin,
        string? email = null,
        bool isPrimary = true,
        bool isOwner = true,
        string? createdBy = null)
    {
        // 1. تحقق
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building ID is required");

        if (string.IsNullOrWhiteSpace(apartmentId))
            throw new ArgumentException("Apartment ID is required");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4 || !pin.All(char.IsDigit))
            throw new ArgumentException("PIN must be exactly 4 digits");

        // 2. جيب الـ Building للتأكد إنه موجود
        var building = await _buildings.GetByIdAsync(buildingId)
            ?? throw new InvalidOperationException("Building not found");

        var apt = building.Apartments.FirstOrDefault(a => a.Id == apartmentId)
            ?? throw new InvalidOperationException("Apartment not found");

        // 3. لو IsPrimary، نتأكد إن مفيش ساكن رئيسي تاني
        if (isPrimary)
        {
            var existing = await _store.GetByApartmentAsync(buildingId, apartmentId);
            foreach (var r in existing.Where(r => r.IsPrimary))
            {
                r.IsPrimary = false;
                r.UpdatedAt = DateTime.UtcNow;
                r.UpdatedBy = createdBy;
                await _store.UpsertAsync(r);
            }
        }

        // 4. أنشئ الـ Resident
        var resident = new ResidentEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            Uid = $"res-{Guid.NewGuid():N}",
            BuildingId = buildingId,
            ApartmentId = apartmentId,
            Name = name.Trim(),
            Phone = AuthHelpers.NormalizePhone(phone),
            Whatsapp = AuthHelpers.NormalizePhone(string.IsNullOrWhiteSpace(whatsapp) ? phone : whatsapp),
            Email = email?.Trim() ?? "",
            PinHash = _hasher.HashPin(pin),
            IsActive = true,
            IsDisabled = false,
            IsOwner = isOwner,
            IsPrimary = isPrimary,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        await _store.UpsertAsync(resident);

        await _audit.LogAsync(
            action: "resident.create",
            buildingId: buildingId,
            apartmentId: apartmentId,
            userId: createdBy,
            userRole: "admin",
            metadata: new { residentId = resident.Id, name, phone, isPrimary },
            severity: "info");

        _log.LogInformation(
            "[ResidentsService] Created resident {Id} in building {Building} apt {Apt}",
            resident.Id, buildingId, apt.Number);

        return resident;
    }

    // ═══════════════════════════════════════════════════════════
    // 3. تعديل ساكن
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> UpdateAsync(
        string residentId,
        string name,
        string phone,
        string whatsapp,
        string? email,
        bool isPrimary,
        bool isOwner,
        string? updatedBy = null)
    {
        var resident = await _store.GetByIdAsync(residentId);
        if (resident == null) return false;

        // لو اتحوّل لساكن رئيسي، نخلي الباقي مش رئيسي
        if (isPrimary && !resident.IsPrimary)
        {
            var siblings = await _store.GetByApartmentAsync(resident.BuildingId, resident.ApartmentId);
            foreach (var s in siblings.Where(s => s.Id != residentId && s.IsPrimary))
            {
                s.IsPrimary = false;
                s.UpdatedAt = DateTime.UtcNow;
                s.UpdatedBy = updatedBy;
                await _store.UpsertAsync(s);
            }
        }

        resident.Name = name.Trim();
        resident.Phone = AuthHelpers.NormalizePhone(phone);
        resident.Whatsapp = AuthHelpers.NormalizePhone(string.IsNullOrWhiteSpace(whatsapp) ? phone : whatsapp);
        resident.Email = email?.Trim() ?? "";
        resident.IsPrimary = isPrimary;
        resident.IsOwner = isOwner;
        resident.UpdatedAt = DateTime.UtcNow;
        resident.UpdatedBy = updatedBy;

        await _store.UpsertAsync(resident);

        await _audit.LogAsync(
            action: "resident.update",
            buildingId: resident.BuildingId,
            apartmentId: resident.ApartmentId,
            userId: updatedBy,
            userRole: "admin",
            metadata: new { residentId, name, phone, isPrimary },
            severity: "info");

        return true;
    }

    // ═══════════════════════════════════════════════════════════
    // 4. تغيير PIN
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> ChangePinAsync(
        string residentId, string newPin, string? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(newPin) || newPin.Length != 4 || !newPin.All(char.IsDigit))
            throw new ArgumentException("PIN must be exactly 4 digits");

        var resident = await _store.GetByIdAsync(residentId);
        if (resident == null) return false;

        resident.PinHash = _hasher.HashPin(newPin);
        resident.UpdatedAt = DateTime.UtcNow;
        resident.UpdatedBy = updatedBy;

        await _store.UpsertAsync(resident);

        await _audit.LogAsync(
            action: "resident.change_pin",
            buildingId: resident.BuildingId,
            apartmentId: resident.ApartmentId,
            userId: updatedBy,
            userRole: "admin",
            metadata: new { residentId },
            severity: "warning");

        return true;
    }

    // ═══════════════════════════════════════════════════════════
    // 5. تفعيل/تعطيل
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> SetDisabledAsync(
        string residentId, bool disabled, string? reason, string? updatedBy = null)
    {
        var resident = await _store.GetByIdAsync(residentId);
        if (resident == null) return false;

        resident.IsDisabled = disabled;
        resident.DisabledReason = disabled ? (reason ?? "") : "";
        resident.UpdatedAt = DateTime.UtcNow;
        resident.UpdatedBy = updatedBy;

        await _store.UpsertAsync(resident);

        await _audit.LogAsync(
            action: disabled ? "resident.disabled" : "resident.enabled",
            buildingId: resident.BuildingId,
            apartmentId: resident.ApartmentId,
            userId: updatedBy,
            userRole: "admin",
            metadata: new { residentId, reason },
            severity: disabled ? "warning" : "info");

        return true;
    }

    // ═══════════════════════════════════════════════════════════
    // 6. حذف
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> DeleteAsync(string residentId, string? deletedBy = null)
    {
        var resident = await _store.GetByIdAsync(residentId);
        if (resident == null) return false;

        await _store.DeleteAsync(residentId);

        await _audit.LogAsync(
            action: "resident.delete",
            buildingId: resident.BuildingId,
            apartmentId: resident.ApartmentId,
            userId: deletedBy,
            userRole: "admin",
            metadata: new { residentId, name = resident.Name },
            severity: "critical");

        return true;
    }

    // ═══════════════════════════════════════════════════════════
    // 7. تحديث آخر تسجيل دخول
    // ═══════════════════════════════════════════════════════════

    public Task UpdateLastLoginAsync(string residentId)
        => _store.UpdateLastLoginAsync(residentId);

    // ═══════════════════════════════════════════════════════════
    // 8. للـ Auth
    // ═══════════════════════════════════════════════════════════

    public async Task<ResidentEntity?> FindByPhoneAsync(string buildingId, string phone)
        => await _store.FindByPhoneAsync(buildingId, phone);

    // ═══════════════════════════════════════════════════════════
    // 9. Migration — من Apartments → Residents
    // ═══════════════════════════════════════════════════════════

    public async Task<MigrationResult> MigrateFromApartmentsAsync(
        string? buildingId = null, bool dryRun = false)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = new MigrationResult();

        try
        {
            var buildings = string.IsNullOrWhiteSpace(buildingId)
                ? await _buildings.GetAllAsync()
                : new List<Building> { await _buildings.GetByIdAsync(buildingId) ?? new() };

            foreach (var building in buildings.Where(b => !string.IsNullOrEmpty(b.Id)))
            {
                result.BuildingsScanned++;

                foreach (var apt in building.Apartments)
                {
                    // ✅ لو مفيش بيانات → تخطى
                    if (string.IsNullOrWhiteSpace(apt.Owner) &&
                        string.IsNullOrWhiteSpace(apt.Phone))
                        continue;

                    // ✅ نتأكد إن الساكن مش موجود في SQL
                    var existing = await _store.GetByApartmentAsync(building.Id, apt.Id);
                    if (existing.Any())
                    {
                        result.EntriesSkipped++;
                        continue;
                    }

                    if (dryRun)
                    {
                        result.EntriesFound++;
                        result.Details.Add(
                            $"Would create: {apt.Owner} (apt {apt.Number}, {apt.Phone})");
                        continue;
                    }

                    // ✅ أنشئ الساكن
                    try
                    {
                        var resident = new ResidentEntity
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Uid = $"res-{Guid.NewGuid():N}",
                            BuildingId = building.Id,
                            ApartmentId = apt.Id,
                            Name = string.IsNullOrWhiteSpace(apt.Owner)
                                ? $"ساكن شقة {apt.Number}"
                                : apt.Owner,
                            Phone = AuthHelpers.NormalizePhone(apt.Phone ?? ""),
                            Whatsapp = AuthHelpers.NormalizePhone(apt.Phone ?? ""),
                            Email = apt.Email ?? "",
                            PinHash = !string.IsNullOrWhiteSpace(apt.Pin)
                                ? _hasher.HashPin(apt.Pin)
                                : _hasher.HashPin("0000"),
                            IsActive = !apt.Disabled,
                            IsDisabled = apt.Disabled,
                            DisabledReason = apt.DisabledReason ?? "",
                            IsOwner = true,
                            IsPrimary = true,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "migration"
                        };

                        await _store.UpsertAsync(resident);
                        result.EntriesMigrated++;
                        result.Details.Add(
                            $"Created: {resident.Name} (apt {apt.Number})");
                    }
                    catch (Exception ex)
                    {
                        result.EntriesFailed++;
                        result.Errors.Add(
                            $"Failed apt {apt.Number} ({apt.Id}): {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Fatal: {ex.Message}");
            _log.LogError(ex, "[Migration] Failed");
        }

        sw.Stop();
        result.Duration = sw.Elapsed;

        _log.LogInformation(
            "[Migration] Done: found={Found}, migrated={Migrated}, skipped={Skipped}, failed={Failed}, duration={Duration}s",
            result.EntriesFound, result.EntriesMigrated, result.EntriesSkipped,
            result.EntriesFailed, sw.Elapsed.TotalSeconds);

        return result;
    }
}