using System.Security.Claims;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

/// <summary>Central SuperAdmin governance for identities, accounts, occupancies and scoped roles.</summary>
[Authorize(Roles = "superadmin")]
[AdminPermission(AdminPermissions.ManageAdmins)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class IdentityGovernanceController : Controller
{
    private readonly IdentityGovernanceService _service;
    public IdentityGovernanceController(IdentityGovernanceService service) => _service = service;

    private bool IsImpersonated => User.HasClaim("impersonated", "true");
    private Guid ActorId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    /// <summary>Displays the identity governance inventory and current account, occupancy and role state.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        if (IsImpersonated || ActorId == Guid.Empty) return Forbid();
        return View(await _service.GetPageAsync(q, ct));
    }

    /// <summary>Creates a person record through the audited identity governance workflow.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePerson(string displayName, string? legalName, string? phone, string? email, CancellationToken ct)
        => await Mutate(() => _service.CreatePersonAsync(displayName, legalName, phone, email, ActorId, ct));

    /// <summary>Updates a person record through the audited identity governance workflow.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePerson(Guid personId, string displayName, string? legalName, string? phone, string? email, CancellationToken ct)
        => await Mutate(() => _service.UpdatePersonAsync(personId, displayName, legalName, phone, email, ActorId, ct));

    /// <summary>Archives or restores a person after validating dependent identity records.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPersonArchived(Guid personId, bool archived, CancellationToken ct)
        => await Mutate(() => _service.SetPersonArchivedAsync(personId, archived, ActorId, ct));

    /// <summary>Creates a pending account for a person with an active occupancy.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePendingAccount(Guid personId, CancellationToken ct)
        => await Mutate(() => _service.CreatePendingAccountAsync(personId, ActorId, ct));

    /// <summary>Creates an apartment occupancy and provisions a pending account when required.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOccupancy(Guid personId, string buildingId, string apartmentId, byte occupancyType, bool isPrimary, DateTime validFromUtc, string? notes, CancellationToken ct)
        => await Mutate(() => _service.CreateOccupancyAsync(personId, buildingId, apartmentId, occupancyType, isPrimary, validFromUtc, notes, ActorId, ct));

    /// <summary>Ends an occupancy and revokes the associated resident access.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EndOccupancy(Guid occupancyId, DateTime validToUtc, CancellationToken ct)
        => await Mutate(() => _service.EndOccupancyAsync(occupancyId, validToUtc, ActorId, ct));

    /// <summary>Grants a role assignment with the requested scope and effective period.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantRole(Guid accountId, string roleKey, byte scopeType, string? buildingId, string? apartmentId, DateTime validFromUtc, DateTime? validToUtc, string reason, CancellationToken ct)
        => await Mutate(() => _service.GrantRoleAsync(accountId, roleKey, scopeType, buildingId, apartmentId, validFromUtc, validToUtc, reason, ActorId, ct));

    /// <summary>Revokes an existing role assignment with an audit reason.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeRole(Guid assignmentId, string reason, CancellationToken ct)
        => await Mutate(() => _service.RevokeRoleAsync(assignmentId, reason, ActorId, ct));

    private async Task<IActionResult> Mutate(Func<Task<(bool Success, string Message)>> operation)
    {
        if (IsImpersonated || ActorId == Guid.Empty) return Forbid();
        try
        {
            var result = await operation();
            TempData[result.Success ? "Message" : "Error"] = result.Message;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            TempData["Error"] = "Unable to complete the operation: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
