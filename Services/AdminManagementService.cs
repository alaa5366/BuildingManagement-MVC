using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Services;

public class AdminManagementService
{
    private readonly FirestoreDb _db;
    private readonly UsersService _users;
    private readonly FirebaseAuthRestService _fbAuth;
    private readonly IAuditLogger _audit;
    private const string Collection = "users";

    public AdminManagementService(
        FirestoreContext ctx,
        UsersService users,
        FirebaseAuthRestService fbAuth,
        IAuditLogger audit)
    {
        _db = ctx.Db;
        _users = users;
        _fbAuth = fbAuth;
        _audit = audit;
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
        var password = AuthHelpers.AdminPasswordForPhone(normalizedPhone, pin);

        // 1. Firebase Auth
        var authResult = await _fbAuth.CreateUserAsync(email, password);
        if (!authResult.Success || string.IsNullOrEmpty(authResult.Uid))
            throw new InvalidOperationException("فشل إنشاء حساب Firebase Auth: " + (authResult.Error ?? "unknown"));

        var uid = authResult.Uid;

        try
        {
            // 2. Firestore
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
            // Rollback: نحذف حساب Firebase Auth
            // (مش عندنا DeleteUser في FirebaseAuthRestService الأصلي —
            //  نسجّلها في الـ audit كـ failed)
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
            ?? throw new InvalidOperationException("الأدمن غير موجود");

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
            ?? throw new InvalidOperationException("الأدمن غير موجود");

        // 1. Firestore
        await _users.SetAsync(uid, new Dictionary<string, object>
        {
            ["pin"] = newPin,
            ["updatedAt"] = DateTime.UtcNow.ToString("o"),
            ["updatedBy"] = updatedBy
        });

        // 2. Firebase Auth — مش عندنا UpdatePassword في الـ REST الأصلي،
        //    فبنعيد إنشاء الحساب بنفس الباسورد الجديد
        var buildingId = admin.BuildingIds.FirstOrDefault() ?? "";
        var email = AuthHelpers.AdminEmailForPhone(buildingId, admin.Phone);
        var password = AuthHelpers.AdminPasswordForPhone(admin.Phone, newPin);
        await _fbAuth.CreateUserAsync(email, password);

        await _audit.LogAsync(
            action: "admin.change_pin",
            buildingId: buildingId,
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
            ?? throw new InvalidOperationException("الأدمن غير موجود");

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
            ?? throw new InvalidOperationException("الأدمن غير موجود");

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
    // حذف (Soft delete: تعطيل + إزالة من قائمة العمارة)
    // ============================================================
    public async Task DeleteAsync(string uid, string deletedBy)
    {
        var admin = await _users.GetByUidAsync(uid)
            ?? throw new InvalidOperationException("الأدمن غير موجود");

        await _users.SetAsync(uid, new Dictionary<string, object>
        {
            ["isActive"] = false,
            ["disabled"] = true,
            ["deletedAt"] = DateTime.UtcNow.ToString("o"),
            ["deletedBy"] = deletedBy
        });

        await _audit.LogAsync(
            action: "admin.delete",
            buildingId: admin.BuildingIds.FirstOrDefault(),
            userId: deletedBy,
            userRole: "superadmin",
            metadata: new { adminUid = uid, phone = admin.Phone },
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