// =====================================================================
//  SqlAuthService — تسجيل دخول من SQL Server مباشرة
// =====================================================================
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class SqlAuthService
{
    private readonly ResidentSqlStore _residents;
    private readonly UserSqlStore _users;
    private readonly BuildingsService _buildings;
    private readonly IPasswordHasher _hasher;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<SqlAuthService> _log;

    private static string[] SuperAdminEmails = Array.Empty<string>();

    public SqlAuthService(
        ResidentSqlStore residents,
        UserSqlStore users,
        BuildingsService buildings,
        IPasswordHasher hasher,
        IDbContextFactory<AppDbContext> sqlFactory,
        ILogger<SqlAuthService> log)
    {
        _residents = residents;
        _users = users;
        _buildings = buildings;
        _hasher = hasher;
        _sqlFactory = sqlFactory;
        _log = log;
    }

    public static void ConfigureSuperAdmins(string commaSeparated)
    {
        SuperAdminEmails = commaSeparated
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToArray();
    }

    public static bool IsSuperAdminEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? "";
        return !string.IsNullOrWhiteSpace(email) && SuperAdminEmails.Contains(normalized);
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Resident Login
    // ═══════════════════════════════════════════════════════════
    public async Task<AuthResult> SignInResidentAsync(
        string buildingId, string floorId, string aptId, string whatsapp, string pin)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            return AuthResult.Fail("building-required");
        if (string.IsNullOrWhiteSpace(floorId))
            return AuthResult.Fail("floor-required");
        if (string.IsNullOrWhiteSpace(aptId))
            return AuthResult.Fail("apartment-required");
        if (string.IsNullOrWhiteSpace(whatsapp))
            return AuthResult.Fail("phone-required");
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4)
            return AuthResult.Fail("pin-required");

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null)
            return AuthResult.Fail("building-not-found");

        // ✅ تحقق من الـ Floor
        var floor = building.Floors.FirstOrDefault(f => f.Id == floorId);
        if (floor == null)
            return AuthResult.Fail("floor-not-found");

        // ✅ تحقق من الشقة
        var apt = building.Apartments.FirstOrDefault(a =>
            a.Id == aptId && a.FloorId == floorId);
        if (apt == null)
            return AuthResult.Fail("apartment-not-found");

        // ✅ ابحث عن الساكن
        var normalizedPhone = AuthHelpers.NormalizePhone(whatsapp);
        var residents = await _residents.GetByApartmentAsync(buildingId, apt.Id);

        var resident = residents.FirstOrDefault(r =>
            r.Phone == normalizedPhone && !r.IsDisabled);

        if (resident == null)
        {
            _log.LogWarning(
                "[SqlAuth] Resident not found: {B}/{F}/{A}/{P}",
                buildingId, floorId, aptId, normalizedPhone);
            return AuthResult.Fail("wrong-credentials");
        }

        if (string.IsNullOrWhiteSpace(resident.PinHash))
            return AuthResult.Fail("pin-not-set");

        if (!_hasher.VerifyPin(pin, resident.PinHash))
        {
            _log.LogWarning("[SqlAuth] Wrong PIN for resident {Uid}", resident.Uid);
            return AuthResult.Fail("wrong-pin");
        }

        try { await _residents.UpdateLastLoginAsync(resident.Id); }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[SqlAuth] UpdateLastLogin failed for {Id}", resident.Id);
        }

        _log.LogInformation(
            "[SqlAuth] Resident: {Name} (apt {Apt}, floor {F})",
            resident.Name, apt.Number, floor.Order);

        return AuthResult.Ok(
            uid: resident.Uid,
            role: "resident",
            email: resident.Email ?? "",
            name: string.IsNullOrWhiteSpace(resident.Name)
                ? $"ساكن شقة {apt.Number}" : resident.Name,
            buildingIds: new List<string> { building.Id },
            aptId: resident.ApartmentId,
            aptNumber: apt.Number);
    }
    // ═══════════════════════════════════════════════════════════
    // 2. Admin Login
    // ═══════════════════════════════════════════════════════════
    public async Task<AuthResult> SignInAdminAsync(
        string buildingId, string phone, string pin)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            return AuthResult.Fail("building-required");
        if (string.IsNullOrWhiteSpace(phone))
            return AuthResult.Fail("phone-required");
        if (string.IsNullOrWhiteSpace(pin))
            return AuthResult.Fail("pin-required");

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null)
            return AuthResult.Fail("building-not-found");

        var normalizedPhone = AuthHelpers.NormalizePhone(phone);

        await using var db = await _sqlFactory.CreateDbContextAsync();

        // ✅ جيب كل الأدمنز بنفس الرقم
        var candidates = await db.Users
            .Include(u => u.Permissions)
            .Include(u => u.BuildingAdmins)
            .Where(u => u.Phone == normalizedPhone && u.Role == "admin")
            .ToListAsync();

        if (candidates.Count == 0)
        {
            _log.LogWarning("[SqlAuth] Admin not found: phone={P}", normalizedPhone);
            return AuthResult.Fail("wrong-credentials");
        }

        // ✅ اختار الأدمن اللي في العمارة المطلوبة
        var admin = candidates.FirstOrDefault(u =>
            u.BuildingAdmins.Any(ba => ba.BuildingId == buildingId));

        if (admin == null)
        {
            _log.LogWarning(
                "[SqlAuth] No admin in building {B} with phone {P}",
                buildingId, normalizedPhone);
            return AuthResult.Fail("not-in-building");
        }

        // ✅ تحقق من الحالة
        if (admin.IsDisabled)
            return AuthResult.Fail("account-disabled", admin.DisabledReason);

        if (!admin.IsActive)
            return AuthResult.Fail("account-inactive");

        // ✅ تحقق من الـ PIN
        if (string.IsNullOrWhiteSpace(admin.Pin))
            return AuthResult.Fail("pin-not-set");

        // دعم الاتنين: Plain + BCrypt
        var pinValid = admin.Pin == pin || _hasher.VerifyPin(pin, admin.Pin);
        if (!pinValid)
        {
            _log.LogWarning(
                "[SqlAuth] Wrong PIN for admin {Uid} in building {B}",
                admin.Uid, buildingId);
            return AuthResult.Fail("wrong-pin");
        }

        _log.LogInformation(
            "[SqlAuth] Admin: {Name} ({Email}) in building {B}",
            admin.Name, admin.Email, building.Name);

        return AuthResult.Ok(
            uid: admin.Uid,
            role: "admin",
            email: admin.Email,
            name: admin.Name,
            buildingIds: admin.BuildingAdmins.Select(ba => ba.BuildingId).ToList());
    }

    // ═══════════════════════════════════════════════════════════
    // 3. Super Admin Login
    // ═══════════════════════════════════════════════════════════
    public async Task<AuthResult> SignInSuperAdminAsync(string email, string password)
    {
        if (!IsSuperAdminEmail(email))
            return AuthResult.Fail("not-superadmin");

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var superAdmin = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == "superadmin");

        if (superAdmin == null)
        {
            _log.LogWarning("[SqlAuth] SuperAdmin not found: {Email}", email);
            return AuthResult.Fail("wrong-credentials");
        }

        if (string.IsNullOrWhiteSpace(superAdmin.Pin))
            return AuthResult.Fail("pin-not-set");

        // ✅ دعم الاتنين
        var passwordValid = superAdmin.Pin == password
            || _hasher.Verify(password, superAdmin.Pin);

        if (!passwordValid)
        {
            _log.LogWarning("[SqlAuth] Wrong password for superadmin: {Email}", email);
            return AuthResult.Fail("wrong-credentials");
        }

        _log.LogInformation("[SqlAuth] SuperAdmin: {Email}", email);

        return AuthResult.Ok(
            uid: superAdmin.Uid,
            role: "superadmin",
            email: email,
            name: string.IsNullOrWhiteSpace(superAdmin.Name)
                ? "Super Admin" : superAdmin.Name,
            buildingIds: new List<string>());
    }

    // ═══════════════════════════════════════════════════════════
    // 4. Unified Login — يجرّب كل الاحتمالات
    // ═══════════════════════════════════════════════════════════
    public async Task<List<UserContext>> FindAllContextsAsync(
        string identifier, string credential)
    {
        var contexts = new List<UserContext>();

        // أ) لو إيميل → Super Admin
        if (identifier.Contains("@"))
        {
            if (IsSuperAdminEmail(identifier))
            {
                var result = await SignInSuperAdminAsync(identifier, credential);
                if (result.Success)
                {
                    contexts.Add(new UserContext
                    {
                        Id = "superadmin",
                        Type = "superadmin",
                        Icon = "👑",
                        Title = "Super Admin",
                        Subtitle = identifier,
                        Email = identifier,
                        Uid = result.Uid,
                        Name = result.Name
                    });
                }
            }
            return contexts;
        }

        // ب) لو رقم → Admin + Resident
        var phone = AuthHelpers.NormalizePhone(identifier);
        if (string.IsNullOrEmpty(phone)) return contexts;

        // فحص الأدمن
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var admins = await db.Users
            .Include(u => u.BuildingAdmins)
            .Where(u => u.Role == "admin" && u.Phone == phone)
            .ToListAsync();

        foreach (var admin in admins)
        {
            if (admin.IsDisabled) continue;

            var pinValid = admin.Pin == credential || _hasher.VerifyPin(credential, admin.Pin);
            if (!pinValid) continue;

            foreach (var ba in admin.BuildingAdmins)
            {
                var building = await _buildings.GetByIdAsync(ba.BuildingId);
                if (building == null) continue;

                contexts.Add(new UserContext
                {
                    Id = $"admin_{ba.BuildingId}",
                    Type = "admin",
                    Icon = "🏢",
                    Title = Loc.T("Admin"),
                    Subtitle = building.Name,
                    BuildingId = ba.BuildingId,
                    BuildingName = building.Name,
                    BuildingNumber = building.BuildingNumber,
                    Email = admin.Email,
                    Uid = admin.Uid,
                    Name = admin.Name
                });
            }
        }

        // فحص السكان
        var buildings = await _buildings.GetAllAsync();
        foreach (var building in buildings)
        {
            var residents = await _residents.GetByBuildingAsync(building.Id);

            foreach (var r in residents)
            {
                if (r.Phone != phone) continue;
                if (r.IsDisabled || !r.IsActive) continue;

                var pinValid = _hasher.VerifyPin(credential, r.PinHash);
                if (!pinValid) continue;

                var apt = building.Apartments.FirstOrDefault(a => a.Id == r.ApartmentId);
                if (apt == null) continue;

                contexts.Add(new UserContext
                {
                    Id = $"resident_{building.Id}_{r.ApartmentId}",
                    Type = "resident",
                    Icon = "🏠",
                    Title = Loc.T("Resident_2"),
                    Subtitle = $"{building.Name} — {apt.Number}",
                    BuildingId = building.Id,
                    BuildingName = building.Name,
                    BuildingNumber = building.BuildingNumber,
                    AptNumber = apt.Number,
                    AptId = r.ApartmentId,
                    AptLabel = apt.Label,
                    Email = r.Email,
                    Uid = r.Uid,
                    Name = r.Name
                });
            }
        }

        return contexts;
    }

    // ═══════════════════════════════════════════════════════════
    // 5. Sign In From Context
    // ═══════════════════════════════════════════════════════════
    public Task<AuthResult> SignInFromContextAsync(UserContext context)
    {
        if (context.Type == "superadmin")
        {
            return Task.FromResult(AuthResult.Ok(
                context.Uid!, "superadmin", context.Email!,
                context.Name ?? "Super Admin", new List<string>()));
        }

        if (context.Type == "admin")
        {
            return Task.FromResult(AuthResult.Ok(
                context.Uid!, "admin", context.Email!,
                context.Name ?? "Admin", new List<string> { context.BuildingId! }));
        }

        if (context.Type == "resident")
        {
            return Task.FromResult(AuthResult.Ok(
                context.Uid!, "resident", context.Email!,
                context.Name ?? "Resident", new List<string> { context.BuildingId! },
                context.AptId, context.AptNumber));
        }

        return Task.FromResult(AuthResult.Fail("unknown-context"));
    }
}