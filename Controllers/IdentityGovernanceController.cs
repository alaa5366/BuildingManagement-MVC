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

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (IsImpersonated || ActorId == Guid.Empty) return Forbid();
        return View(await _service.GetPageAsync(ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePerson(string displayName, string? legalName, string? phone, string? email, CancellationToken ct)
        => await Mutate(() => _service.CreatePersonAsync(displayName, legalName, phone, email, ActorId, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePerson(Guid personId, string displayName, string? legalName, string? phone, string? email, CancellationToken ct)
        => await Mutate(() => _service.UpdatePersonAsync(personId, displayName, legalName, phone, email, ActorId, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPersonArchived(Guid personId, bool archived, CancellationToken ct)
        => await Mutate(() => _service.SetPersonArchivedAsync(personId, archived, ActorId, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOccupancy(Guid personId, string buildingId, string apartmentId, byte occupancyType, bool isPrimary, DateTime validFromUtc, string? notes, CancellationToken ct)
        => await Mutate(() => _service.CreateOccupancyAsync(personId, buildingId, apartmentId, occupancyType, isPrimary, validFromUtc, notes, ActorId, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EndOccupancy(Guid occupancyId, DateTime validToUtc, CancellationToken ct)
        => await Mutate(() => _service.EndOccupancyAsync(occupancyId, validToUtc, ActorId, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantRole(Guid accountId, string roleKey, byte scopeType, string? buildingId, string? apartmentId, DateTime validFromUtc, DateTime? validToUtc, string reason, CancellationToken ct)
        => await Mutate(() => _service.GrantRoleAsync(accountId, roleKey, scopeType, buildingId, apartmentId, validFromUtc, validToUtc, reason, ActorId, ct));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeRole(Guid assignmentId, string reason, CancellationToken ct)
        => await Mutate(() => _service.RevokeRoleAsync(assignmentId, reason, ActorId, ct));

    private async Task<IActionResult> Mutate(Func<Task<(bool Success, string Message)>> operation)
    {
        if (IsImpersonated || ActorId == Guid.Empty) return Forbid();
        var result = await operation();
        TempData[result.Success ? "Message" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
