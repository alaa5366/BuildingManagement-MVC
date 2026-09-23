namespace BuildingManagementMvc.Services;

// ترجمة حرفية لمنطق تسجيل الدخول الثلاثي في js/services/auth-service.js
// (Super Admin بالإيميل، Admin برقم الهاتف + PIN، Resident بالدور +
// رقم الشقة + واتساب + PIN)
public class AuthService
{
    private static readonly string[] SuperAdminEmails =
    {
        "alaa5366@gmail.com",
        "alaa5366@hotmail.com"
    };

    private readonly BuildingsService _buildings;
    private readonly UsersService _users;
    private readonly FirebaseAuthRestService _fbAuth;

    public AuthService(BuildingsService buildings, UsersService users, FirebaseAuthRestService fbAuth)
    {
        _buildings = buildings;
        _users = users;
        _fbAuth = fbAuth;
    }

    public static bool IsSuperAdminEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && SuperAdminEmails.Contains(email.Trim().ToLowerInvariant());

    public async Task<AuthResult> SignInSuperAdminAsync(string email, string password)
    {
        if (!IsSuperAdminEmail(email)) return AuthResult.Fail("not-superadmin");

        var res = await _fbAuth.SignInWithPasswordAsync(email, password);
        if (!res.Success) return AuthResult.Fail(res.Error ?? "signin-failed");

        var existing = await _users.GetByUidAsync(res.Uid!);
        if (existing == null || existing.Role != "superadmin")
        {
            await _users.SetAsync(res.Uid!, new Dictionary<string, object>
            {
                ["email"] = email,
                ["name"] = existing?.Name is { Length: > 0 } n ? n : "Super Admin",
                ["role"] = "superadmin",
                ["buildingIds"] = new List<object>()
            });
        }

        return AuthResult.Ok(res.Uid!, "superadmin", email, existing?.Name ?? "Super Admin", new List<string>());
    }

    public async Task<AuthResult> SignInAdminAsync(string buildingId, string phone, string pin)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return AuthResult.Fail("building-not-found");

        var normPhone = AuthHelpers.NormalizePhone(phone);
        var admins = await _users.GetAdminsAsync();
        if (admins.Count == 0) return AuthResult.Fail("no-admin");

        foreach (var admin in admins)
        {
            if (!admin.BuildingIds.Contains(building.Id)) continue;

            var userPhone = AuthHelpers.NormalizePhone(admin.Phone);
            if (userPhone != normPhone || admin.Pin != pin) continue;

            if (admin.Disabled) return AuthResult.Fail("account-disabled", admin.DisabledReason);

            var password = AuthHelpers.AdminPasswordForPhone(admin.Phone, pin);
            var signIn = await _fbAuth.SignInWithPasswordAsync(admin.Email, password);
            if (!signIn.Success) return AuthResult.Fail(signIn.Error ?? "signin-failed");

            return AuthResult.Ok(signIn.Uid!, "admin", admin.Email, admin.Name, new List<string> { building.Id });
        }

        return AuthResult.Fail("wrong-credentials");
    }

    public async Task<AuthResult> SignInResidentAsync(string buildingId, int floorOrder, int aptNumber, string whatsapp, string pin)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return AuthResult.Fail("building-not-found");

        var floor = building.Floors.FirstOrDefault(f => f.Order == floorOrder);
        var apt = floor == null
            ? null
            : building.Apartments.FirstOrDefault(a => a.FloorId == floor.Id && a.Number == aptNumber);

        if (apt == null) return AuthResult.Fail("apt-not-found");

        var normWa = AuthHelpers.NormalizePhone(whatsapp);
        if (AuthHelpers.NormalizePhone(apt.Phone) != normWa) return AuthResult.Fail("wrong-wa");
        if (apt.Pin != pin) return AuthResult.Fail("wrong-pin");
        if (apt.Disabled) return AuthResult.Fail("account-disabled", apt.DisabledReason);

        var email = AuthHelpers.ResidentInternalEmail(building.Id, floor!.Order, aptNumber);
        var password = AuthHelpers.ResidentPassword(building.Id, aptNumber, pin);
        var signIn = await _fbAuth.SignInWithPasswordAsync(email, password);
        if (!signIn.Success) return AuthResult.Fail(signIn.Error ?? "signin-failed");

        var name = string.IsNullOrWhiteSpace(apt.Owner) ? $"شقة {aptNumber}" : apt.Owner;
        return AuthResult.Ok(signIn.Uid!, "resident", email, name, new List<string> { building.Id }, apt.Id, aptNumber);
    }
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Reason { get; set; }
    public string? Uid { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public List<string> BuildingIds { get; set; } = new();
    public string? ApartmentId { get; set; }
    public int? ApartmentNumber { get; set; }

    public static AuthResult Ok(string uid, string role, string email, string name, List<string> buildingIds,
        string? aptId = null, int? aptNumber = null) => new()
        {
            Success = true, Uid = uid, Role = role, Email = email, Name = name,
            BuildingIds = buildingIds, ApartmentId = aptId, ApartmentNumber = aptNumber
        };

    public static AuthResult Fail(string error, string? reason = null) =>
        new() { Success = false, Error = error, Reason = reason };
}
