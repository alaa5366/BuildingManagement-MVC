using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

[Authorize]
public class PresenceController : Controller
{
    private readonly PresenceService _presence;
    private readonly ILogger<PresenceController> _logger;

    public PresenceController(PresenceService presence, ILogger<PresenceController> logger)
    {
        _presence = presence;
        _logger = logger;
    }

    // ============================================================
    // POST /Presence/Heartbeat
    // بدون CSRF — لأنها عملية غير حساسة
    // ============================================================
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