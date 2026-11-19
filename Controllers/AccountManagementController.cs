using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountManagementController : Controller
{
    private readonly AccountManagementService _service;

    public AccountManagementController(AccountManagementService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, string? role, string? status,
        int page = 1, int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (User.HasClaim("impersonated", "true"))
            return Forbid();

        // Bound user-controlled query values before they reach in-memory filtering.
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim()[..Math.Min(search.Trim().Length, 128)];
        role = role is "resident" or "admin" or "superadmin" ? role : null;
        status = status is "active" or "inactive" ? status : null;

        var model = await _service.GetPageAsync(search, role, status, page, pageSize, cancellationToken);
        return View(model);
    }
}
