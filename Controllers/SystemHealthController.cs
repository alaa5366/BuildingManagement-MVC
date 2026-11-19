using System.Diagnostics;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingManagementMvc.Controllers;

/// <summary>Displays a safe, administrator-only view of registered system health checks.</summary>
[Authorize(Roles = "superadmin,admin")]
[AdminPermission(AdminPermissions.ViewApplicationLogs)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SystemHealthController : Controller
{
    private readonly HealthCheckService _healthChecks;
    private readonly TimeProvider _timeProvider;
    private static readonly DateTimeOffset ProcessStartedAt =
        new(Process.GetCurrentProcess().StartTime.ToUniversalTime());

    public SystemHealthController(HealthCheckService healthChecks, TimeProvider timeProvider)
    {
        _healthChecks = healthChecks;
        _timeProvider = timeProvider;
    }

    [HttpGet]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var report = await _healthChecks.CheckHealthAsync(cancellationToken: cancellationToken);
        var model = new SystemHealthViewModel
        {
            CheckedAtUtc = _timeProvider.GetUtcNow(),
            ProcessStartedAtUtc = ProcessStartedAt,
            TotalDuration = report.TotalDuration,
            OverallStatus = report.Status.ToString(),
            Checks = report.Entries.OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new SystemHealthCheckViewModel
                {
                    Name = x.Key,
                    Status = x.Value.Status.ToString(),
                    Duration = x.Value.Duration,
                    Tags = x.Value.Tags.OrderBy(t => t, StringComparer.Ordinal).ToArray()
                }).ToList()
        };

        ViewBag.CanViewAuditTrail = User.IsInRole("superadmin") ||
            User.HasClaim("perm", AdminPermissions.ViewAuditLogBuilding);

        return View(model);
    }
}
