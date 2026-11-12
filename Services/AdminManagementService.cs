using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class AdminManagementService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly UsersService _users;
    private readonly IAuditLogger _audit;
    private readonly ILogger<AdminManagementService> _log;

    public AdminManagementService(
        IDbContextFactory<AppDbContext> sqlFactory,
        UsersService users,
        IAuditLogger audit,
        ILogger<AdminManagementService>? log = null)
    {
        _sqlFactory = sqlFactory;
        _users = users;
        _audit = audit;
        _log = log ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminManagementService>.Instance;
    }

    public async Task<List<AppUserDoc>> GetAdminsAsync(string? buildingId = null)
    {
        var admins = await _users.GetAdminsAsync();
        if (!string.IsNullOrWhiteSpace(buildingId))
            admins = admins.Where(a => a.BuildingIds.Contains(buildingId)).ToList();
        return admins.OrderBy(a => a.Name).ToList();
    }

    public async Task<AppUserDoc?> GetAdminAsync(string uid) => await _users.GetByUidAsync(uid);

    public async Task<string> CreateAdminAsync(
        string name, string phone, string buildingId, string pin,
        List<string> permissions, string createdBy)
    {
        var normalizedPhone = AuthHelpers.NormalizePhone(phone);
        var email = AuthHelpers.AdminEmailForPhone(buildingId, normalizedPhone);
        var uid = Guid.NewGuid().ToString("N");

        try
        {
            await _users.SetAsync(uid, new Dictionary<string, object>
            {
                ["email"] = email,
                ["name"] = name,
                ["phone"] = normalizedPhone,
                ["whatsapp"] = normalizedPhone,
                ["pin"] = pin,
                ["role"] = "admin",
                ["buildingIds"] = new List<string> { buildingId },
                ["permissions"] = permissions ?? new List<string>(),
                ["isActive"] = true,
                ["disabled"] = false,
                ["createdAt"] = DateTime.UtcNow.ToString("o"),
                ["createdBy"] = createdBy
            });

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

    public async Task UpdateAdminAsync(
        string uid, string name, string phone, List<string> permissions, bool isActive, string updatedBy)
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

    public async Task DeleteAsync(string uid, string deletedBy)
    {
        var admin = await _users.GetByUidAsync(uid)
            ?? throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));

        var buildingId = admin.BuildingIds.FirstOrDefault() ?? "";
        var email = admin.Email ?? "";
        var phone = admin.Phone ?? "";

        await using var db = await _sqlFactory.CreateDbContextAsync();

        await db.BuildingAdmins.Where(ba => ba.AdminUid == uid).ExecuteDeleteAsync();
        await db.UserPermissions.Where(p => p.Uid == uid).ExecuteDeleteAsync();
        await db.Users.Where(u => u.Uid == uid).ExecuteDeleteAsync();

        await _audit.LogAsync(
            action: "admin.delete",
            buildingId: buildingId,
            userId: deletedBy,
            userRole: "superadmin",
            metadata: new { adminUid = uid, phone, email, hardDelete = true },
            severity: "critical");
    }

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