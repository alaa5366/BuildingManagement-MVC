using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// =====================================================================
//  ImpersonationService — Nested Cookie Stack
//  ✅ بيدعم طبقات متعددة: SuperAdmin → Admin → Resident → ...
// =====================================================================
public class ImpersonationService
{
    private const string BackupCookieName = "bm_admin_backup_stack";
    private const int MaxStackDepth = 5;

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
        _protector = dataProtection
            .CreateProtector("BuildingManagementMvc.Impersonation.v2")
            .ToTimeLimitedDataProtector();
    }

    // ============================================================
    // 1. قراءة الـ Stack كامل
    // ============================================================
    public List<AdminBackupData> ReadBackupStack(HttpContext ctx)
    {
        if (!ctx.Request.Cookies.TryGetValue(BackupCookieName, out var json))
            return new List<AdminBackupData>();

        try
        {
            var decrypted = _protector.Unprotect(json);
            var stack = JsonSerializer.Deserialize<List<AdminBackupData>>(decrypted);
            return stack ?? new List<AdminBackupData>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Impersonation] Failed to read backup stack");
            return new List<AdminBackupData>();
        }
    }

    // ============================================================
    // 2. Push (إضافة طبقة جديدة)
    // ============================================================
    public void PushBackup(HttpContext ctx, AdminBackupData data)
    {
        var stack = ReadBackupStack(ctx);

        if (stack.Count >= MaxStackDepth)
        {
            _logger.LogWarning("[Impersonation] Stack depth limit ({Max}) reached", MaxStackDepth);
            return;
        }

        stack.Add(data);
        SaveStack(ctx, stack);

        _logger.LogInformation(
            "[Impersonation] Push: depth={Depth}, role={Role}",
            stack.Count, data.Role);
    }

    // ============================================================
    // 3. Pop (إزالة آخر طبقة)
    // ============================================================
    public AdminBackupData? PopBackup(HttpContext ctx)
    {
        var stack = ReadBackupStack(ctx);
        if (stack.Count == 0) return null;

        var last = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        SaveStack(ctx, stack);

        _logger.LogInformation(
            "[Impersonation] Pop: depth={Depth}, popped={Role}",
            stack.Count, last.Role);

        return last;
    }

    // ============================================================
    // 4. Peek (قراءة آخر طبقة بدون إزالة)
    // ============================================================
    public AdminBackupData? PeekBackup(HttpContext ctx)
    {
        var stack = ReadBackupStack(ctx);
        return stack.Count > 0 ? stack[^1] : null;
    }

    // ============================================================
    // 5. حالة الـ Impersonation
    // ============================================================
    public bool IsImpersonating(HttpContext ctx)
    {
        return ctx.User.HasClaim("impersonated", "true");
    }

    public int StackDepth(HttpContext ctx) => ReadBackupStack(ctx).Count;

    // ============================================================
    // 6. حفظ الـ Stack في الـ Cookie
    // ============================================================
    private void SaveStack(HttpContext ctx, List<AdminBackupData> stack)
    {
        if (stack.Count == 0)
        {
            ctx.Response.Cookies.Delete(BackupCookieName);
            return;
        }

        var json = JsonSerializer.Serialize(stack);
        var encrypted = _protector.Protect(json, TimeSpan.FromHours(2));

        ctx.Response.Cookies.Append(BackupCookieName, encrypted, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(2),
            IsEssential = true
        });
    }

    // ============================================================
    // 7. مسح الـ Stack كامل
    // ============================================================
    public void ClearStack(HttpContext ctx)
    {
        ctx.Response.Cookies.Delete(BackupCookieName);
    }

    // ============================================================
    // 8. Audit Log — دخول
    // ============================================================
    public async Task LogEnterAsync(Building building, Apartment apt, string adminUid, string adminName)
    {
        await _audit.LogAsync(
            action: "impersonation.enter",
            buildingId: building.Id,
            apartmentId: apt.Id,
            userId: adminUid,
            userRole: "admin",
            metadata: new { adminName, aptNumber = apt.Number, aptOwner = apt.Owner },
            severity: "critical");
    }

    // ============================================================
    // 9. Audit Log — خروج
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
    // 10. Audit Log — SuperAdmin يدخل كأدمن
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
// AdminBackupData — بيانات الطبقة
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