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

    /// <summary>Displays the management page and its current records.</summary>
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

    /// <summary>Updates the enabled status of a resident account.</summary>
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


    /// <summary>Updates the status of a managed account.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetManagedAccountStatus(Guid accountId, bool activate, CancellationToken cancellationToken)
    {
        if (User.HasClaim("impersonated", "true")) return Forbid();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Forbid();

        var result = await _service.SetManagedAccountStatusAsync(accountId, activate, actorId, cancellationToken);
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Resets the PIN for a managed account.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetManagedAccountPin(Guid accountId, CancellationToken cancellationToken)
    {
        if (User.HasClaim("impersonated", "true")) return Forbid();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Forbid();

        var result = await _service.ResetManagedAccountPinAsync(accountId, actorId, _hasher, cancellationToken);
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        if (result.Success && !string.IsNullOrWhiteSpace(result.TemporaryPin))
            TempData["TemporaryManagedAccountPin"] = result.TemporaryPin;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Updates the status of a legacy super-administrator account.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLegacySuperAdminStatus(string legacyUserId, bool activate, CancellationToken cancellationToken)
    {
        if (User.HasClaim("impersonated", "true")) return Forbid();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Forbid();

        var result = await _service.SetLegacySuperAdminStatusAsync(legacyUserId, activate, actorId, cancellationToken);
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Resets the password for a legacy super-administrator account.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetLegacySuperAdminPassword(string legacyUserId, CancellationToken cancellationToken)
    {
        if (User.HasClaim("impersonated", "true")) return Forbid();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId)) return Forbid();

        var result = await _service.ResetLegacySuperAdminPasswordAsync(legacyUserId, actorId, _hasher, cancellationToken);
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        if (result.Success && !string.IsNullOrWhiteSpace(result.TemporaryPassword))
            TempData["TemporarySuperAdminPassword"] = result.TemporaryPassword;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Resets the PIN for a resident account.</summary>
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
