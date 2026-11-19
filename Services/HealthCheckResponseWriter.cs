using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Writes a stable, intentionally minimal health response without exposing exception details.
/// </summary>
public static class HealthCheckResponseWriter
{
    /// <summary>
    /// Writes the aggregate health status and individual check names/statuses as JSON.
    /// </summary>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString()
            })
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
