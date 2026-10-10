// =====================================================================
//  PinMigrationService — تحويل PINs Plain → BCrypt
//  بيشتغل على جدول Users (Admin + SuperAdmin)
// =====================================================================
using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class PinMigrationService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<PinMigrationService> _log;

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
public class PinMigrationResult
{
    public bool Success { get; set; }
    public int TotalScanned { get; set; }
    public int Migrated { get; set; }
    public int AlreadyHashed { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public int WouldMigrate { get; set; }
    public List<string> Details { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}