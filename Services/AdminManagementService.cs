using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BuildingManagementMvc.Services;

public class AdminManagementService
{
    private readonly FirestoreDb _db;
    private readonly UsersService _users;
    private readonly FirebaseAdminService _fbAdmin;
    private readonly IAuditLogger _audit;
    private readonly IDbContextFactory<AppDbContext>? _sqlFactory;
    private readonly ILogger<AdminManagementService> _log;
    private const string Collection = "users";

    public AdminManagementService(
        FirestoreContext ctx,
        UsersService users,
        FirebaseAdminService fbAdmin,
        IAuditLogger audit,
        IDbContextFactory<AppDbContext>? sqlFactory = null,
        ILogger<AdminManagementService>? log = null)
    {
        _db = ctx.Db;
        _users = users;
        _fbAdmin = fbAdmin;
        _audit = audit;
        _sqlFactory = sqlFactory;
        _log = log ?? NullLogger<AdminManagementService>.Instance;
    }

    // ============================================================
    // قراءة
    // ============================================================
    public async Task<List<AppUserDoc>> GetAdminsAsync(string? buildingId = null)
    {
        var admins = await _users.GetAdminsAsync();
        if (!string.IsNullOrWhiteSpace(buildingId))
            admins = admins.Where(a => a.BuildingIds.Contains(buildingId)).ToList();
        return admins.OrderBy(a => a.Name).ToList();
    }

    public async Task<AppUserDoc?> GetAdminAsync(string uid)
        => await _users.GetByUidAsync(uid);

    // ============================================================
    // إنشاء أدمن
    // ============================================================
    public async Task<string> CreateAdminAsync(
        string name, string phone, string buildingId, string pin,
        List<string> permissions, string createdBy)
    {
        var normalizedPhone = AuthHelpers.NormalizePhone(phone);
        var email = AuthHelpers.AdminEmailForPhone(buildingId, normalizedPhone);

        // ✅ Uid جديد (بدل Firebase Auth)
        var uid = Guid.NewGuid().ToString("N");

        try
        {
            // 1. Firestore (Users)
            await _users.SetAsync(uid, new Dictionary<string, object>
            {
                ["email"] = email,
                ["name"] = name,
                ["phone"] = normalizedPhone,
                ["whatsapp"] = normalizedPhone,
                ["pin"] = pin,
                ["role"] = "admin",
                ["buildingIds"] = new List<object> { buildingId },
                ["permissions"] = permissions ?? new List<string>(),
                ["isActive"] = true,
                ["disabled"] = false,
                ["createdAt"] = DateTime.UtcNow.ToString("o"),
                ["createdBy"] = createdBy
            });

            // 2. SQL (Users + BuildingAdmins)
            if (_sqlFactory != null)
            {
                try
                {
                    await using var db = await _sqlFactory.CreateDbContextAsync();

                    // Users
                    var userEntity = new Data.Entities.UserEntity
                    {
                        Uid = uid,
                        Email = email,
                        Name = name,
                        Phone = normalizedPhone,
                        Whatsapp = normalizedPhone,
                        Pin = pin,
                        Role = "admin",
                        IsActive = true,
                        IsDisabled = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = createdBy
                    };
                    db.Users.Add(userEntity);

                    // BuildingAdmins
                    db.BuildingAdmins.Add(new Data.Entities.BuildingAdminEntity
                    {
                        BuildingId = buildingId,
                        AdminUid = uid
                    });

                    // Permissions
                    if (permissions != null)
                    {
                        foreach (var p in permissions)
                        {
                            db.UserPermissions.Add(new Data.Entities.UserPermissionEntity
                            {
                                Uid = uid,
                                Permission = p
                            });
                        }
                    }

                    await db.SaveChangesAsync();

                    _log.LogInformation("[AdminManagement] Created admin in SQL: {Uid}", uid);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "SQL insert failed for admin {Uid}", uid);
                }
            }

            // 3. Audit
            await _audit.LogAsync(
                action: "admin.create",
                buildingId: buildingId,
                userId: createdBy,
                userRole: "superadmin",
                metadata: new { adminUid = uid, phone = normalizedPhone, email },
                severity: "critical");

            return uid;
        }
        catch
        {
            await _audit.LogAsync(
                action: "admin.create.failed",
                buildingId: buildingId,
                userId: createdBy,
                userRole: "superadmin",
                metadata: new { phone = normalizedPhone, email },
                severity: "critical");
            throw;
        }
    }

    // ============================================================
    // تعديل أدمن
    // ============================================================
    public async Task UpdateAdminAsync(
        string uid, string name, string phone,
        List<string> permissions, bool isActive, string updatedBy)
    {
        var existing = await _users.GetByUidAsync(uid)
            ?? throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));

        await _users.SetAsync(uid, new Dictionary<string, object>
        {
            ["name"] = name,
            ["phone"] = AuthHelpers.NormalizePhone(phone),
            ["permissions"] = permissions ?? new List<string>(),
            ["isActive"] = isActive,
            ["updatedAt"] = DateTime.UtcNow.ToString("o"),
            ["updatedBy"] = updatedBy
        });

        await _audit.LogAsync(
            action: "admin.update",
            buildingId: existing.BuildingIds.FirstOrDefault(),
            userId: updatedBy,
            userRole: "superadmin",
            metadata: new { adminUid = uid, isActive },
            severity: "critical");
    }

    // ============================================================
    // تغيير PIN
    // ============================================================
    public async Task ChangePinAsync(string uid, string newPin, string updatedBy)
    {
        var admin = await _users.GetByUidAsync(uid)
            ?? throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));

        await _users.SetAsync(uid, new Dictionary<string, object>
        {
            ["pin"] = newPin,
            ["updatedAt"] = DateTime.UtcNow.ToString("o"),
            ["updatedBy"] = updatedBy
        });

        await _audit.LogAsync(
            action: "admin.change_pin",
            buildingId: admin.BuildingIds.FirstOrDefault(),
            userId: updatedBy,
            userRole: "superadmin",
            metadata: new { adminUid = uid },
            severity: "critical");
    }

    // ============================================================
    // تعطيل / تنشيط
    // ============================================================
    public async Task DeactivateAsync(string uid, string updatedBy)
    {
        var admin = await _users.GetByUidAsync(uid)
            ?? throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));

        await _users.SetAsync(uid, new Dictionary<string, object>
        {
            ["isActive"] = false,
            ["disabled"] = true,
            ["updatedAt"] = DateTime.UtcNow.ToString("o"),
            ["updatedBy"] = updatedBy
        });

        await _audit.LogAsync(
            action: "admin.deactivate",
            buildingId: admin.BuildingIds.FirstOrDefault(),
            userId: updatedBy,
            userRole: "superadmin",
            metadata: new { adminUid = uid },
            severity: "critical");
    }

    public async Task ActivateAsync(string uid, string updatedBy)
    {
        var admin = await _users.GetByUidAsync(uid)
            ?? throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));

        await _users.SetAsync(uid, new Dictionary<string, object>
        {
            ["isActive"] = true,
            ["disabled"] = false,
            ["updatedAt"] = DateTime.UtcNow.ToString("o"),
            ["updatedBy"] = updatedBy
        });

        await _audit.LogAsync(
            action: "admin.activate",
            buildingId: admin.BuildingIds.FirstOrDefault(),
            userId: updatedBy,
            userRole: "superadmin",
            metadata: new { adminUid = uid },
            severity: "critical");
    }

    // ============================================================
    // حذف نهائي
    // ============================================================
    public async Task DeleteAsync(string uid, string deletedBy)
    {
        var admin = await _users.GetByUidAsync(uid)
            ?? throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));

        var buildingId = admin.BuildingIds.FirstOrDefault() ?? "";
        var email = admin.Email ?? "";
        var phone = admin.Phone ?? "";

        if (_sqlFactory != null)
        {
            try
            {
                await using var db = await _sqlFactory.CreateDbContextAsync();

                var buildingAdmins = await db.BuildingAdmins
                    .Where(ba => ba.AdminUid == uid)
                    .ToListAsync();
                if (buildingAdmins.Any())
                    db.BuildingAdmins.RemoveRange(buildingAdmins);

                var permissions = await db.UserPermissions
                    .Where(p => p.Uid == uid)
                    .ToListAsync();
                if (permissions.Any())
                    db.UserPermissions.RemoveRange(permissions);

                var user = await db.Users.FirstOrDefaultAsync(u => u.Uid == uid);
                if (user != null)
                    db.Users.Remove(user);

                await db.SaveChangesAsync();

                _log.LogInformation("✅ Hard delete from SQL: {Uid}", uid);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "⚠️ SQL delete failed for admin {Uid}", uid);
            }
        }

        try
        {
            await _db.Collection(Collection).Document(uid).DeleteAsync();
            _log.LogInformation("✅ Hard delete from Firestore: {Uid}", uid);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "⚠️ Firestore delete failed: {Uid}", uid);
        }

        await _audit.LogAsync(
            action: "admin.delete",
            buildingId: buildingId,
            userId: deletedBy,
            userRole: "superadmin",
            metadata: new { adminUid = uid, phone, email, hardDelete = true },
            severity: "critical");
    }

    // ============================================================
    // مزامنة الصلاحيات
    // ============================================================
    public async Task SyncPermissionsAsync(string uid, List<string> permissions, string updatedBy)
    {
        var admin = await _users.GetByUidAsync(uid);
        if (admin == null) return;

        await _users.SetAsync(uid, new Dictionary<string, object>
        {
            ["permissions"] = permissions ?? new List<string>(),
            ["updatedAt"] = DateTime.UtcNow.ToString("o"),
            ["updatedBy"] = updatedBy
        });

        await _audit.LogAsync(
            action: "admin.sync_permissions",
            buildingId: admin.BuildingIds.FirstOrDefault(),
            userId: updatedBy,
            userRole: "superadmin",
            metadata: new { adminUid = uid, count = permissions?.Count ?? 0 },
            severity: "warning");
    }
}