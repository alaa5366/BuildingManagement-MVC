// =====================================================================
//  PinMigrationService — تحويل PINs Plain → BCrypt
//  بيشتغل على جدول Users (Admin + SuperAdmin)
// =====================================================================
using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Startup migration that hashes any plain-text PINs and clears obsolete plain-text PIN columns.
/// </summary>
public class PinMigrationService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<PinMigrationService> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="PinMigrationService"/> class.
    /// </summary>
    public PinMigrationService(
        IDbContextFactory<AppDbContext> sqlFactory,
        IPasswordHasher hasher,
        ILogger<PinMigrationService> log)
    {
        _sqlFactory = sqlFactory;
        _hasher = hasher;
        _log = log;
    }

    // ═══════════════════════════════════════════════════════════
    // Migration — كل الـ Users اللي عندهم PIN Plain
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Migrates the all.
    /// </summary>
    public async Task<PinMigrationResult> MigrateAllAsync(bool dryRun = false)
    {
        var result = new PinMigrationResult();

        await using var db = await _sqlFactory.CreateDbContextAsync();

        var users = await db.Users
            .Where(u => u.Role == "admin" || u.Role == "superadmin")
            .ToListAsync();

        result.TotalScanned = users.Count;

        foreach (var user in users)
        {
            try
            {
                // ✅ Skip لو الـ PIN فاضي
                if (string.IsNullOrWhiteSpace(user.Pin))
                {
                    result.Skipped++;
                    continue;
                }

                // ✅ Skip لو الـ PIN بالفعل BCrypt
                if (IsBcrypt(user.Pin))
                {
                    result.AlreadyHashed++;
                    continue;
                }

                // ✅ Skip لو الـ PIN مش 4 أرقام (للـ SuperAdmin ممكن يكون password)
                if (user.Role == "admin" && user.Pin.Length != 4)
                {
                    result.Failed++;
                    result.Errors.Add($"{user.Email}: PIN مش 4 أرقام");
                    continue;
                }

                // ✅ في Dry Run — ما نعملش hash
                if (dryRun)
                {
                    result.WouldMigrate++;
                    result.Details.Add($"Would migrate: {user.Email} ({user.Role})");
                    continue;
                }

                // ✅ اعمل hash + احفظ
                var oldPin = user.Pin;
                user.Pin = user.Role == "admin"
                    ? _hasher.HashPin(oldPin)
                    : _hasher.Hash(oldPin);

                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = "pin-migration";

                result.Migrated++;
                result.Details.Add($"Migrated: {user.Email} ({user.Role})");

                _log.LogInformation(
                    "[PinMigration] Migrated {Email} ({Role})",
                    user.Email, user.Role);
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"{user.Email}: {ex.Message}");
                _log.LogError(ex, "[PinMigration] Failed for {Email}", user.Email);
            }
        }

        if (!dryRun)
            await db.SaveChangesAsync();

        result.Success = result.Failed == 0;

        _log.LogInformation(
            "[PinMigration] Done: scanned={Scanned}, migrated={Migrated}, alreadyHashed={Already}, skipped={Skipped}, failed={Failed}",
            result.TotalScanned, result.Migrated, result.AlreadyHashed,
            result.Skipped, result.Failed);

        return result;
    }

    // ═══════════════════════════════════════════════════════════
    // بيشتغل عند إقلاع التطبيق: أي PIN نص صريح (أدمن/سوبر أدمن/ساكن) يتحوّل BCrypt،
    // وأي Building.AdminPin قديم بيتمسح.
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Migrates the legacy secrets.
    /// </summary>
    public async Task<(int Users, int Residents, int BuildingPinsCleared)> MigrateLegacySecretsAsync()
    {
        await using var db = await _sqlFactory.CreateDbContextAsync();

        var users = await db.Users
            .Where(u => (u.Role == "admin" || u.Role == "superadmin") && u.Pin != "")
            .ToListAsync();

        var migratedUsers = 0;
        foreach (var u in users.Where(x => !SecretVerifier.IsBcrypt(x.Pin)))
        {
            u.Pin = SecretVerifier.Hash(u.Pin);
            u.UpdatedAt = DateTime.UtcNow;
            u.UpdatedBy = "pin-migration";
            migratedUsers++;
        }

        var residents = await db.Residents.Where(r => r.PinHash != "").ToListAsync();
        var migratedResidents = 0;
        foreach (var r in residents.Where(x => !SecretVerifier.IsBcrypt(x.PinHash)))
        {
            r.PinHash = SecretVerifier.Hash(r.PinHash);
            migratedResidents++;
        }

        if (migratedUsers + migratedResidents > 0)
            await db.SaveChangesAsync();

        var cleared = await db.Buildings
            .Where(b => b.AdminPin != "")
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.AdminPin, ""));

        // Migrate legacy apartment PINs into a primary resident account before clearing plaintext.
        var legacyApartments = await db.Apartments
            .Where(a => a.Pin != "")
            .ToListAsync();
        var clearedApts = 0;
        foreach (var apartment in legacyApartments)
        {
            var existingResidents = await db.Residents
                .Where(r => r.BuildingId == apartment.BuildingId && r.ApartmentId == apartment.Id)
                .ToListAsync();
            if (existingResidents.Count == 0)
            {
                db.Residents.Add(new BuildingManagementMvc.Data.Entities.ResidentEntity
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Uid = Guid.NewGuid().ToString("N"),
                    BuildingId = apartment.BuildingId,
                    ApartmentId = apartment.Id,
                    Name = apartment.Owner ?? string.Empty,
                    Phone = apartment.Phone ?? string.Empty,
                    Whatsapp = apartment.Phone ?? string.Empty,
                    Email = apartment.Email ?? string.Empty,
                    PinHash = SecretVerifier.Hash(apartment.Pin),
                    IsPrimary = true, IsOwner = true, IsActive = true,
                    CreatedAt = DateTime.UtcNow, CreatedBy = "pin-migration"
                });
                migratedResidents++;
            }
            else
            {
                var blankPinResidents = existingResidents.Where(r => string.IsNullOrWhiteSpace(r.PinHash)).ToList();
                if (blankPinResidents.Count > 0)
                {
                    var hashedLegacyPin = SecretVerifier.Hash(apartment.Pin);
                    foreach (var resident in blankPinResidents) resident.PinHash = hashedLegacyPin;
                    migratedResidents += blankPinResidents.Count;
                }
            }
            // Clear only after a resident account exists for the same apartment.
            apartment.Pin = string.Empty;
            clearedApts++;
        }
        if (clearedApts > 0) await db.SaveChangesAsync();

        _log.LogInformation(
            "[PinMigration] startup: users={U}, residents={R}, buildingAdminPinsCleared={B}, apartmentPinsCleared={A}",
            migratedUsers, migratedResidents, cleared, clearedApts);

        return (migratedUsers, migratedResidents, cleared);
    }

    // ═══════════════════════════════════════════════════════════
    // Helper — هل النص BCrypt؟
    // ═══════════════════════════════════════════════════════════
    private static bool IsBcrypt(string value)
    {
        // BCrypt hash يبدأ بـ $2a$, $2b$, $2y$
        return value.StartsWith("$2a$") ||
               value.StartsWith("$2b$") ||
               value.StartsWith("$2y$");
    }
}

// ═══════════════════════════════════════════════════════════════
// Result
// ═══════════════════════════════════════════════════════════════
/// <summary>
/// Counts reported by the PIN migration.
/// </summary>
public class PinMigrationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether it is success.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets the total scanned.
    /// </summary>
    public int TotalScanned { get; set; }
    /// <summary>
    /// Gets or sets the migrated.
    /// </summary>
    public int Migrated { get; set; }
    /// <summary>
    /// Gets or sets the already hashed.
    /// </summary>
    public int AlreadyHashed { get; set; }
    /// <summary>
    /// Gets or sets the skipped.
    /// </summary>
    public int Skipped { get; set; }
    /// <summary>
    /// Gets or sets the failed.
    /// </summary>
    public int Failed { get; set; }
    /// <summary>
    /// Gets or sets the would migrate.
    /// </summary>
    public int WouldMigrate { get; set; }
    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    public List<string> Details { get; set; } = new();
    /// <summary>
    /// Gets or sets the errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}