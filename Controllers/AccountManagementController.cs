using System.Security.Claims;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountManagementController : Controller
{
    private readonly AccountManagementService _service;
    private readonly IPasswordHasher _hasher;

    public AccountManagementController(AccountManagementService service, IPasswordHasher hasher)
    {
        _service = service;
        _hasher = hasher;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, string? role, string? status,
        int page = 1, int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (User.HasClaim("impersonated", "true")) return Forbid();
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 128)];
        role = role is "resident" or "admin" or "superadmin" ? role : null;
        status = status is "active" or "inactive" ? status : null;
        return View(await _service.GetPageAsync(search, role, status, page, pageSize, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetResidentStatus(Guid accountId, bool activate, CancellationToken cancellationToken)
    {
        if (User.HasClaim("impersonated", "true")) return Forbid();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Forbid();

        var result = await _service.SetResidentStatusAsync(accountId, activate, actorId, cancellationToken);
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetResidentPin(Guid accountId, CancellationToken cancellationToken)
    {
        if (User.HasClaim("impersonated", "true")) return Forbid();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Forbid();

        var result = await _service.ResetResidentPinAsync(accountId, actorId, _hasher, cancellationToken);
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        if (result.Success && !string.IsNullOrWhiteSpace(result.TemporaryPin))
            TempData["TemporaryResidentPin"] = result.TemporaryPin;
        return RedirectToAction(nameof(Index));
    }
}
