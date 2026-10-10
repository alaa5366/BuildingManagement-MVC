using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

public class ImpersonationService
{
    private const string BackupCookieName = "bm_admin_backup_stack";
    private const string ItemsKey = "impersonation_stack";
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
    // 1. قراءة الـ Stack (من Items أو Cookie)
    // ============================================================
    public List<AdminBackupData> ReadBackupStack(HttpContext ctx)
    {
        // ✅ الأول: من Items (نفس الطلب)
        if (ctx.Items.TryGetValue(ItemsKey, out var cached) && cached is List<AdminBackupData> cachedStack)
        {
            return new List<AdminBackupData>(cachedStack);
        }

        // ✅ الثاني: من Request Cookie
        if (!ctx.Request.Cookies.TryGetValue(BackupCookieName, out var json))
            return new List<AdminBackupData>();

        try
        {
            var decrypted = _protector.Unprotect(json);
            var stack = JsonSerializer.Deserialize<List<AdminBackupData>>(decrypted)
                ?? new List<AdminBackupData>();

            // ✅ خزّن في Items
            ctx.Items[ItemsKey] = stack;
            return new List<AdminBackupData>(stack);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Impersonation] Failed to read backup stack");
            return new List<AdminBackupData>();
        }
    }

    // ============================================================
    // 2. Push
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
    // 3. Pop
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
    // 4. Peek
    // ============================================================
    public AdminBackupData? PeekBackup(HttpContext ctx)
    {
        var stack = ReadBackupStack(ctx);
        return stack.Count > 0 ? stack[^1] : null;
    }

    // ============================================================
    // 5. IsImpersonating
    // ============================================================
    public bool IsImpersonating(HttpContext ctx)
    {
        return ctx.User.HasClaim("impersonated", "true");
    }

    public int StackDepth(HttpContext ctx) => ReadBackupStack(ctx).Count;

    // ============================================================
    // 6. SaveStack (في Items + Response Cookie)
    // ============================================================
    private void SaveStack(HttpContext ctx, List<AdminBackupData> stack)
    {
        // ✅ احفظ في Items للطلبات الجاية في نفس الطلب
        ctx.Items[ItemsKey] = new List<AdminBackupData>(stack);

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
    // 7. ClearStack
    // ============================================================
    public void ClearStack(HttpContext ctx)
    {
        ctx.Items.Remove(ItemsKey);
        ctx.Response.Cookies.Delete(BackupCookieName);
    }

    // ============================================================
    // 8. Audit Logs
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