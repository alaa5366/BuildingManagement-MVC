using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

/// <summary>Super-admin-only reporting and controlled batch provisioning for resident accounts.</summary>
[Authorize(Roles = "superadmin")]
public sealed class ResidentAccountProvisioningController : Controller
{
    private readonly ResidentAccountProvisioningService _provisioning;

    public ResidentAccountProvisioningController(ResidentAccountProvisioningService provisioning)
    {
        _provisioning = provisioning;
    }

    /// <summary>Displays the resident account provisioning report.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.HasClaim("impersonated", "true"))
            return Forbid();

        var report = await _provisioning.GetReportAsync(cancellationToken);
        return View(report);
    }

    /// <summary>Creates a controlled batch of eligible resident accounts.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Provision(int batchSize = 25, CancellationToken cancellationToken = default)
    {
        if (User.HasClaim("impersonated", "true"))
            return Forbid();

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (string.IsNullOrWhiteSpace(actorId))
            return Forbid();

        var result = await _provisioning.ProvisionBatchAsync(batchSize, actorId, cancellationToken);
        TempData["Message"] = $"تم إنشاء {result.Created} حساب، وتجاوز {result.Skipped} حالة بعد إعادة التحقق. المتبقي المؤهل: {result.RemainingEligible}.";
        return RedirectToAction(nameof(Index));
    }
}
