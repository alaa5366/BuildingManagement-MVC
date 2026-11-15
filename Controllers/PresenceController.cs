using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  PresenceController	تتبع "متصل الآن"
// ============================================================
/// <summary>
/// Heartbeat endpoint used to show which users are currently online.
/// </summary>
/// <remarks>
/// Access: any signed-in user (roles are checked per action).
/// </remarks>
[Authorize]
public class PresenceController : Controller
{
    private readonly PresenceService _presence;
    private readonly ILogger<PresenceController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresenceController"/> class.
    /// </summary>
    public PresenceController(PresenceService presence, ILogger<PresenceController> logger)
    {
        _presence = presence;
        _logger = logger;
    }

    // ============================================================
    // POST /Presence/Heartbeat
    // بدون CSRF — لأنها عملية غير حساسة
    // ============================================================
    /// <summary>
    /// Records that the signed-in user is online. The browser calls it periodically.
    /// </summary>
    /// <remarks>
    /// HTTP POST.
    /// </remarks>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Heartbeat()
    {
        var uid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid))
            return Json(new { success = false, error = "not-authenticated" });

        var buildingId = User.FindFirst("buildingId")?.Value ?? "";
        var apartmentId = User.FindFirst("apartmentId")?.Value ?? "";

        await _presence.HeartbeatAsync(uid, apartmentId, buildingId);

        return Json(new
        {
            success = true,
            ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }
}