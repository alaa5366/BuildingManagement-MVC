using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Keeps the stack of identities for impersonation (super admin to admin to resident) in a protected, time-limited cookie.
/// </summary>
public class ImpersonationService
{
    private const string BackupCookieName = "bm_admin_backup_stack";
    private const string ItemsKey = "impersonation_stack";
    private const int MaxStackDepth = 5;

    private readonly IAuditLogger _audit;
    private readonly ILogger<ImpersonationService> _logger;
    private readonly ITimeLimitedDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImpersonationService"/> class.
    /// </summary>
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
    /// <summary>
    /// Reads the backup stack.
    /// </summary>
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
    /// <summary>
    /// Pushes the backup.
    /// </summary>
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
    /// <summary>
    /// Pops the backup.
    /// </summary>
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
    /// <summary>
    /// Returns the peek backup.
    /// </summary>
    public AdminBackupData? PeekBackup(HttpContext ctx)
    {
        var stack = ReadBackupStack(ctx);
        return stack.Count > 0 ? stack[^1] : null;
    }

    // ============================================================
    // 5. IsImpersonating
    // ============================================================
    /// <summary>
    /// Determines whether the value is impersonating.
    /// </summary>
    public bool IsImpersonating(HttpContext ctx)
    {
        return ctx.User.HasClaim("impersonated", "true");
    }

    /// <summary>
    /// Returns the stack depth.
    /// </summary>
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
    /// <summary>
    /// Clears the stack.
    /// </summary>
    public void ClearStack(HttpContext ctx)
    {
        ctx.Items.Remove(ItemsKey);
        ctx.Response.Cookies.Delete(BackupCookieName);
    }

    // ============================================================
    // 8. Audit Logs
    // ============================================================
    /// <summary>
    /// Logs the enter.
    /// </summary>
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

    /// <summary>
    /// Logs the exit.
    /// </summary>
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

    /// <summary>
    /// Logs the enter building admin.
    /// </summary>
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

/// <summary>
/// Snapshot of the original identity stored while impersonating another user.
/// </summary>
public class AdminBackupData
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Uid { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = "";
    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    public string Role { get; set; } = "";
    /// <summary>
    /// Gets or sets the building IDs.
    /// </summary>
    public List<string> BuildingIds { get; set; } = new();
    /// <summary>
    /// Gets or sets the permissions.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
    /// <summary>
    /// Gets or sets the started date and time.
    /// </summary>
    public string StartedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the original initiating identity UID for nested impersonation restoration.
    /// </summary>
    public string ImpersonationActorUid { get; set; } = "";
    /// <summary>
    /// Gets or sets the original initiating identity role for nested impersonation restoration.
    /// </summary>
    public string ImpersonationActorRole { get; set; } = "";
    /// <summary>
    /// Gets or sets the original initiating identity email for nested impersonation restoration.
    /// </summary>
    public string ImpersonationActorEmail { get; set; } = "";
    /// <summary>
    /// Gets or sets the serialized permissions of the initiating identity for nested impersonation restoration.
    /// </summary>
    public string ImpersonationActorPermissions { get; set; } = "";
}