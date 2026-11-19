// =====================================================================
//  SqlAuthService — تسجيل دخول من SQL Server مباشرة
// =====================================================================
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Authenticates residents, admins and super admins against the database and builds the list of accounts a phone/e-mail can enter.
/// </summary>
public class SqlAuthService
{
    private readonly ResidentSqlStore _residents;
    private readonly UserSqlStore _users;
    private readonly BuildingsService _buildings;
    private readonly IPasswordHasher _hasher;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<SqlAuthService> _log;

    private static string[] SuperAdminEmails = Array.Empty<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlAuthService"/> class.
    /// </summary>
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

    /// <summary>
    /// Sets the list of e-mail addresses that are allowed to sign in as super admin.
    /// </summary>
    public static void ConfigureSuperAdmins(string commaSeparated)
    {
        SuperAdminEmails = commaSeparated
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToArray();
    }

    private static bool VerifySecret(string? input, string? stored) => SecretVerifier.Verify(input, stored);
    /// <summary>Builds a non-reversible session version for legacy SuperAdmin credentials and status.</summary>
    public static string CreateSuperAdminSecurityStamp(string? credentialHash, DateTime? updatedAt, bool isActive, bool isDisabled)
    {
        var material = string.Join("|", credentialHash ?? "", updatedAt?.Ticks ?? 0, isActive, isDisabled);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(material)));
    }


    /// <summary>
    /// Determines whether an e-mail address is in the configured super-admin list.
    /// </summary>
    public static bool IsSuperAdminEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? "";
        return !string.IsNullOrWhiteSpace(email) && SuperAdminEmails.Contains(normalized);
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Resident Login
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Signs in a resident by building, floor, apartment, phone number and PIN. Unknown accounts and wrong PINs return the same error and take the same time.
    /// </summary>
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
            SecretVerifier.BurnTime();
            return AuthResult.Fail("wrong-credentials");
        }

        if (string.IsNullOrWhiteSpace(resident.PinHash))
            return AuthResult.Fail("pin-not-set");

        if (!VerifySecret(pin, resident.PinHash))
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
                ? Loc.T("Resident_Of_Apartment_N", apt.Number) : resident.Name,
            buildingIds: new List<string> { building.Id },
            aptId: resident.ApartmentId,
            aptNumber: apt.Number);
    }
    // ═══════════════════════════════════════════════════════════
    // 2. Admin Login
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Signs in an admin of a specific building by phone number and PIN. The PIN is checked before the account status so the response does not reveal that the account exists.
    /// </summary>
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
            SecretVerifier.BurnTime();
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
            SecretVerifier.BurnTime();
            return AuthResult.Fail("not-in-building");
        }

        // الـ PIN الأول، وبعدها حالة الحساب (عشان رسالة الخطأ ماتكشفش إن الحساب موجود)
        if (string.IsNullOrWhiteSpace(admin.Pin))
            return AuthResult.Fail("pin-not-set");

        if (!VerifySecret(pin, admin.Pin))
        {
            _log.LogWarning(
                "[SqlAuth] Wrong PIN for admin {Uid} in building {B}",
                admin.Uid, buildingId);
            return AuthResult.Fail("wrong-pin");
        }

        if (admin.IsDisabled)
            return AuthResult.Fail("account-disabled", admin.DisabledReason);

        if (!admin.IsActive)
            return AuthResult.Fail("account-inactive");

        _log.LogInformation(
            "[SqlAuth] Admin: {Name} ({Email}) in building {B}",
            admin.Name, admin.Email, building.Name);

        return AuthResult.Ok(
            uid: admin.Uid,
            role: "admin",
            email: admin.Email,
            name: admin.Name,
            // الجلسة مربوطة بالعمارة اللي سجّل دخول عليها بس (كان بياخد أول عمارة من القايمة)
            buildingIds: new List<string> { buildingId },
            permissions: admin.Permissions.Select(p => p.Permission)
                .Distinct(StringComparer.Ordinal).ToList());
    }
    // ═══════════════════════════════════════════════════════════
    // 3. Super Admin Login
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Signs in a super admin by e-mail and password. The e-mail must be in the configured super-admin list.
    /// </summary>
    public async Task<AuthResult> SignInSuperAdminAsync(string email, string password)
    {
        if (!IsSuperAdminEmail(email))
            return AuthResult.Fail("not-superadmin");

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var superAdmin = await db.Users
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.Email == email && u.Role == "superadmin");

        if (superAdmin == null)
        {
            _log.LogWarning("[SqlAuth] SuperAdmin not found: {Email}", email);
            return AuthResult.Fail("wrong-credentials");
        }

        if (string.IsNullOrWhiteSpace(superAdmin.Pin))
            return AuthResult.Fail("pin-not-set");

        // ✅ دعم الاتنين
        var passwordValid = VerifySecret(password, superAdmin.Pin);

        if (!passwordValid)
        {
            _log.LogWarning("[SqlAuth] Wrong password for superadmin: {Email}", email);
            return AuthResult.Fail("wrong-credentials");
        }

        if (superAdmin.IsDisabled)
            return AuthResult.Fail("account-disabled", superAdmin.DisabledReason);
        if (!superAdmin.IsActive)
            return AuthResult.Fail("account-inactive");

        _log.LogDebug("[SqlAuth] SuperAdmin sign-in succeeded");

        var result = AuthResult.Ok(
            uid: superAdmin.Uid,
            role: "superadmin",
            email: email,
            name: string.IsNullOrWhiteSpace(superAdmin.Name)
                ? "Super Admin" : superAdmin.Name,
            buildingIds: new List<string>(),
            permissions: superAdmin.Permissions.Select(p => p.Permission).Distinct(StringComparer.Ordinal).ToList());
        result.SecurityStamp = CreateSuperAdminSecurityStamp(
            superAdmin.Pin, superAdmin.UpdatedAt ?? superAdmin.CreatedAt, superAdmin.IsActive, superAdmin.IsDisabled);
        return result;
    }

    // ═══════════════════════════════════════════════════════════
    // 4. Unified Login — يجرّب كل الاحتمالات
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Finds every account (admin of a building, resident of an apartment) that matches an identifier and credential, for the unified login page.
    /// </summary>
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
            else
            {
                SecretVerifier.BurnTime();
            }
            return contexts;
        }

        // ب) لو رقم → Admin + Resident
        var phone = AuthHelpers.NormalizePhone(identifier);
        if (string.IsNullOrEmpty(phone)) return contexts;

        await using var db = await _sqlFactory.CreateDbContextAsync();

        // ==========================================
        // 1. فحص الأدمن (Admins)
        // ==========================================
        var admins = await db.Users
            .Include(u => u.BuildingAdmins)
            .Where(u => u.Role == "admin" && u.Phone == phone)
            .ToListAsync();

        var verified = false;   // هل عملنا BCrypt فعلًا؟ (لو لأ نعمل واحد وهمي في الآخر)

        foreach (var admin in admins)
        {
            if (admin.IsDisabled) continue;

            verified = true;
            if (!VerifySecret(credential, admin.Pin)) continue;

            var adminBuildingIds = admin.BuildingAdmins.Select(ba => ba.BuildingId).ToList();
            var adminBuildings = await db.Buildings.AsNoTracking()
                .Where(b => adminBuildingIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Name, b.BuildingNumber })
                .ToListAsync();

            foreach (var building in adminBuildings)
            {
                contexts.Add(new UserContext
                {
                    Id = $"admin_{building.Id}",
                    Type = "admin",
                    Icon = "🏢",
                    Title = Loc.T("Admin"),
                    Subtitle = building.Name,
                    BuildingId = building.Id,
                    BuildingName = building.Name,
                    BuildingNumber = building.BuildingNumber,
                    Email = admin.Email,
                    Uid = admin.Uid,
                    Name = admin.Name
                });
            }
        }

        // ==========================================
        // 2. فحص السكان (Residents) — استعلام مباشر بالرقم بدل تحميل كل العمارات
        // ==========================================
        var residentCandidates = await db.Residents.AsNoTracking()
            .Where(r => r.Phone == phone && r.IsActive && !r.IsDisabled)
            .ToListAsync();

        foreach (var r in residentCandidates)
        {
            verified = true;
            if (!VerifySecret(credential, r.PinHash)) continue;

            var apt = await db.Apartments.AsNoTracking()
                .Where(a => a.BuildingId == r.BuildingId && a.Id == r.ApartmentId)
                .Select(a => new { a.Number, a.Label })
                .FirstOrDefaultAsync();
            if (apt == null) continue;

            var building = await db.Buildings.AsNoTracking()
                .Where(b => b.Id == r.BuildingId)
                .Select(b => new { b.Id, b.Name, b.BuildingNumber })
                .FirstOrDefaultAsync();
            if (building == null) continue;

            contexts.Add(new UserContext
            {
                Id = $"resident_{building.Id}_{r.ApartmentId}",
                Type = "resident",
                Icon = "🏠",
                Title = Loc.T("Resident_2"),
                Subtitle = building.Name + " — " + Loc.T("Apartment_N", apt.Number),
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

        if (!verified) SecretVerifier.BurnTime();

        return contexts;
    }

    // ═══════════════════════════════════════════════════════════
    // 5. Sign In From Context
    // ═══════════════════════════════════════════════════════════
    /// <summary>
    /// Signs the in from context.
    /// </summary>
    public async Task<AuthResult> SignInFromContextAsync(UserContext context)
    {
        // The context is a short-lived selection hint, not an authorization snapshot.
        // Re-read the selected account and its current grants before issuing a cookie.
        if (context.Type == "superadmin")
        {
            if (string.IsNullOrWhiteSpace(context.Uid) || string.IsNullOrWhiteSpace(context.Email)
                || !IsSuperAdminEmail(context.Email))
                return AuthResult.Fail("unknown-context");

            await using var db = await _sqlFactory.CreateDbContextAsync();
            var account = await db.Users
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(u => u.Uid == context.Uid
                    && u.Email == context.Email && u.Role == "superadmin");

            if (account == null || !account.IsActive || account.IsDisabled)
                return AuthResult.Fail("account-inactive");

            return AuthResult.Ok(
                account.Uid, "superadmin", account.Email,
                account.Name ?? "Super Admin", new List<string>(),
                permissions: account.Permissions.Select(p => p.Permission)
                    .Distinct(StringComparer.Ordinal).ToList());
        }

        if (context.Type == "admin")
        {
            var admin = await _users.GetByUidAsync(context.Uid!);
            if (admin == null || admin.Role != "admin" || admin.IsDisabled || !admin.IsActive
                || !admin.BuildingAdmins.Any(ba => ba.BuildingId == context.BuildingId))
                return AuthResult.Fail("account-inactive");

            return AuthResult.Ok(
                context.Uid!, "admin", context.Email!,
                context.Name ?? "Admin", new List<string> { context.BuildingId! },
                permissions: admin.Permissions.Select(p => p.Permission)
                    .Distinct(StringComparer.Ordinal).ToList());
        }

        if (context.Type == "resident")
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var resident = await db.Residents.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Uid == context.Uid
                    && r.BuildingId == context.BuildingId
                    && r.ApartmentId == context.AptId
                    && r.IsActive && !r.IsDisabled);
            if (resident == null) return AuthResult.Fail("account-inactive");

            return AuthResult.Ok(
                resident.Uid, "resident", resident.Email ?? "",
                resident.Name ?? "Resident", new List<string> { context.BuildingId! },
                context.AptId, context.AptNumber);
        }

        return AuthResult.Fail("unknown-context");
    }
}