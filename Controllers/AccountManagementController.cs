using System.Security.Claims;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin")]
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
    public async Task<IActionResult> Index(string? search, string? role, string? status, int page = 1,
        int pageSize = 25, CancellationToken cancellationToken = default)
    {
        if (User.HasClaim("impersonated", "true")) return Forbid();
        ViewBag.AccountMessage = TempData["AccountMessage"];
        ViewBag.TemporaryPin = TempData["TemporaryPin"];
        return View(await _service.GetPageAsync(search, role, status, page, pageSize, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Activate(Guid accountId, CancellationToken cancellationToken) =>
        MutateAsync(() => _service.SetResidentStatusAsync(accountId, true, ActorId(), cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Deactivate(Guid accountId, CancellationToken cancellationToken) =>
        MutateAsync(() => _service.SetResidentStatusAsync(accountId, false, ActorId(), cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ResetPin(Guid accountId, CancellationToken cancellationToken) =>
        MutateAsync(() => _service.ResetResidentPinAsync(accountId, ActorId(), _hasher, cancellationToken));

    private string ActorId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "superadmin";

    private async Task<IActionResult> MutateAsync(Func<Task<AccountManagementMutationResult>> operation)
    {
        if (User.HasClaim("impersonated", "true")) return Forbid();
        var result = await operation();
        TempData["AccountMessage"] = result.Message;
        if (result.TemporaryPin is not null) TempData["TemporaryPin"] = result.TemporaryPin;
        return RedirectToAction(nameof(Index));
    }
}
