using System.Text.Json;
using BuildingManagementMvc.Models;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingManagementMvc.Services;

public class UnifiedAuthService
{
    private readonly AuthService _auth;
    private readonly UsersService _users;
    private readonly BuildingsService _buildings;
    private readonly FirebaseAuthRestService _fbAuth;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UnifiedAuthService> _logger;

    public UnifiedAuthService(
        AuthService auth,
        UsersService users,
        BuildingsService buildings,
        FirebaseAuthRestService fbAuth,
        IMemoryCache cache,
        ILogger<UnifiedAuthService> logger)
    {
        _auth = auth;
        _users = users;
        _buildings = buildings;
        _fbAuth = fbAuth;
        _cache = cache;
        _logger = logger;
    }

    // ============================================================
    // 1. البحث عن كل السياقات للمستخدم
    // ============================================================
    public async Task<List<UserContext>> FindAllContextsAsync(string identifier, string credential)
    {
        var contexts = new List<UserContext>();

        // أ) لو إيميل → Super Admin
        if (identifier.Contains("@"))
        {
            if (AuthService.IsSuperAdminEmail(identifier))
            {
                var res = await _fbAuth.SignInWithPasswordAsync(identifier, credential);
                if (res.Success)
                {
                    contexts.Add(new UserContext
                    {
                        Id = "superadmin",
                        Type = "superadmin",
                        Icon = "👑",
                        Title = "Super Admin",
                        Subtitle = identifier,
                        Email = identifier,
                        Uid = res.Uid,
                        Name = "Super Admin"
                    });
                }
            }
            return contexts;
        }

        // ب) لو رقم → فحص Admin + Resident
        var phone = AuthHelpers.NormalizePhone(identifier);
        if (string.IsNullOrEmpty(phone)) return contexts;

        // ---------- فحص الأدمن ----------
        var admins = await _users.GetAdminsAsync();
        foreach (var admin in admins)
        {
            if (AuthHelpers.NormalizePhone(admin.Phone) != phone) continue;
            if (admin.Pin != credential) continue;
            if (admin.Disabled) continue;

            // ✅ لازم نتأكد من Firebase Auth (نحاول ندخل مرة)
            var adminPassword = AuthHelpers.AdminPasswordForPhone(admin.Phone, credential);
            var signIn = await _fbAuth.SignInWithPasswordAsync(admin.Email, adminPassword);
            if (!signIn.Success) continue;

            foreach (var bId in admin.BuildingIds)
            {
                var building = await _buildings.GetByIdAsync(bId);
                if (building == null) continue;

                contexts.Add(new UserContext
                {
                    Id = $"admin_{bId}",
                    Type = "admin",
                    Icon = "🏢",
                    Title = "أدمن",
                    Subtitle = building.Name,
                    BuildingId = bId,
                    BuildingName = building.Name,
                    BuildingNumber = building.BuildingNumber,
                    Email = admin.Email,
                    Uid = signIn.Uid,
                    Name = admin.Name
                });
            }
        }

        // ---------- فحص السكان ----------
        var buildings = await _buildings.GetAllAsync();
        foreach (var building in buildings)
        {
            foreach (var apt in building.Apartments)
            {
                Console.WriteLine($"[UnifiedAuth] Checking apt {apt.Number}, phone={apt.Phone}, pin={apt.Pin}");

                if (AuthHelpers.NormalizePhone(apt.Phone) != phone) continue;
                if (apt.Pin != credential) continue;

                Console.WriteLine($"[UnifiedAuth] Matched apt {apt.Number}!");

                if (apt.Disabled) continue;
                if (apt.Closed) continue;

                var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
                if (floor == null) continue;

                // ✅ نتأكد من Firebase Auth
                var email = AuthHelpers.ResidentInternalEmail(building.Id, floor.Order, apt.Number);
                var password = AuthHelpers.ResidentPassword(building.Id, apt.Number, credential);
                var signIn = await _fbAuth.SignInWithPasswordAsync(email, password);

                Console.WriteLine($"[UnifiedAuth] Auth result for apt {apt.Number}: {signIn.Success}, error: {signIn.Error}");

                if (!signIn.Success) continue;

                contexts.Add(new UserContext
                {
                    Id = $"resident_{building.Id}_{apt.Id}",
                    Type = "resident",
                    Icon = "🏠",
                    Title = "ساكن",
                    Subtitle = $"{building.Name} — شقة {apt.Number}",
                    BuildingId = building.Id,
                    BuildingName = building.Name,
                    BuildingNumber = building.BuildingNumber,
                    FloorOrder = floor.Order,
                    AptNumber = apt.Number,
                    AptLabel = apt.Label,
                    Email = email,
                    Uid = signIn.Uid,
                    Name = string.IsNullOrWhiteSpace(apt.Owner) ? $"شقة {apt.Number}" : apt.Owner
                });
            }
        }

        return contexts;
    }

    // ============================================================
    // 2. دخول مباشر من سياق محدد
    // ============================================================
    public async Task<AuthResult> SignInFromContextAsync(UserContext context)
    {
        if (context.Type == "superadmin")
        {
            return AuthResult.Ok(
                context.Uid!,
                "superadmin",
                context.Email!,
                context.Name ?? "Super Admin",
                new List<string>());
        }

        if (context.Type == "admin")
        {
            return AuthResult.Ok(
                context.Uid!,
                "admin",
                context.Email!,
                context.Name ?? "Admin",
                new List<string> { context.BuildingId! });
        }

        if (context.Type == "resident")
        {
            return AuthResult.Ok(
                context.Uid!,
                "resident",
                context.Email!,
                context.Name ?? "Resident",
                new List<string> { context.BuildingId! },
                null,   // ApartmentId — نحتاجها
                context.AptNumber);
        }

        return AuthResult.Fail("unknown-context");
    }

    // ============================================================
    // 3. حفظ السياقات مؤقتاً (في Memory Cache)
    // ============================================================
    public string StoreContexts(List<UserContext> contexts, string fullName)
    {
        var token = Guid.NewGuid().ToString("N");
        var data = new { Contexts = contexts, FullName = fullName };

        _cache.Set($"ctx_{token}", data, TimeSpan.FromMinutes(10));
        return token;
    }

    public (List<UserContext>? Contexts, string? FullName) GetStoredContexts(string token)
    {
        if (_cache.TryGetValue($"ctx_{token}", out var data))
        {
            dynamic? d = data;
            if (d != null)
            {
                return (d.Contexts as List<UserContext>, d.FullName as string);
            }
        }
        return (null, null);
    }

    public void RemoveStoredContexts(string token)
    {
        _cache.Remove($"ctx_{token}");
    }
}