using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ✅ Phase 24.4 — Impersonation
public class ImpersonationService
{
    private const string BackupCookieName = "bm_admin_backup";
    private readonly IAuditLogger _audit;
    private readonly ILogger<ImpersonationService> _logger;
    private readonly ITimeLimitedDataProtector _protector;

    public ImpersonationService(
        IAuditLogger audit,
        ILogger<ImpersonationService> logger,
        IDataProtectionProvider dataProtection)
    {
        _audit = audit;
        _logger = logger;
        // ✅ الكوكي مشفّر ومتوقّع (Data Protection) وبينتهي بعد ساعتين — مينفعش يتزوّر من المتصفح
        _protector = dataProtection
            .CreateProtector("BuildingManagementMvc.Impersonation.v1")
            .ToTimeLimitedDataProtector();
    }

    // ============================================================
    // 1. قراءة بيانات الأدمن من الكوكي
    // ============================================================
    public AdminBackupData? ReadBackup(HttpContext ctx)
    {
        if (!ctx.Request.Cookies.TryGetValue(BackupCookieName, out var json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<AdminBackupData>(_protector.Unprotect(json));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Impersonation] Failed to deserialize backup cookie");
            return null;
        }
    }

    // ============================================================
    // 2. حفظ بيانات الأدمن في الكوكي
    // ============================================================
    public void SaveBackup(HttpContext ctx, AdminBackupData data)
    {
        var json = _protector.Protect(JsonSerializer.Serialize(data), TimeSpan.FromHours(2));

        ctx.Response.Cookies.Append(BackupCookieName, json, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(2),
            IsEssential = true
        });
    }

    // ============================================================
    // 3. مسح بيانات الأدمن
    // ============================================================
    public void ClearBackup(HttpContext ctx)
    {
        ctx.Response.Cookies.Delete(BackupCookieName);
    }

    // ============================================================
    // 4. هل السياق الحالي Impersonation؟
    // ============================================================
    public bool IsImpersonating(HttpContext ctx)
    {
        // ✅ المصدر الموثوق هو الـ claim اللي السيرفر وقّعه، مش وجود كوكي
        return ctx.User.HasClaim("impersonated", "true");
    }

    // ============================================================
    // 5. Audit Log — دخول
    // ============================================================
    public async Task LogEnterAsync(Building building, Apartment apt, string adminUid, string adminName)
    {
        await _audit.LogAsync(
            action: "impersonation.enter",
            buildingId: building.Id,
            apartmentId: apt.Id,
            userId: adminUid,
            userRole: "admin",
            metadata: new
            {
                adminName,
                aptNumber = apt.Number,
                aptOwner = apt.Owner
            },
            severity: "critical");
    }

    // ============================================================
    // 6. Audit Log — خروج
    // ============================================================
    public async Task LogExitAsync(string buildingId, string adminUid, string adminName)
    {
        await _audit.LogAsync(
            action: "impersonation.exit",
            buildingId: buildingId,
            userId: adminUid,
            userRole: "admin",
            metadata: new { adminName },
            severity: "warning");
    }
    // ============================================================
    // Audit — SuperAdmin يدخل كأدمن على عمارة
    // ============================================================
    public async Task LogEnterBuildingAdminAsync(Building building, string superAdminUid, string superAdminName)
    {
        await _audit.LogAsync(
            action: "impersonation.enter_as_admin",
            buildingId: building.Id,
            userId: superAdminUid,
            userRole: "superadmin",
            metadata: new
            {
                superAdminName,
                buildingName = building.Name,
                buildingNumber = building.BuildingNumber
            },
            severity: "critical");
    }
}



// ============================================================
// ✅ Model — بيانات الأدمن المحفوظة
// ============================================================
public class AdminBackupData
{
    public string Uid { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public List<string> BuildingIds { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public string StartedAt { get; set; } = "";
    public string BuildingId { get; set; } = "";
}