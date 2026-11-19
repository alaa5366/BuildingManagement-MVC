using System.Diagnostics;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Assigns a server-generated correlation identifier to each HTTP request.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string ResponseHeaderName = "X-Correlation-ID";
    public const string ActivityTagName = "correlation.id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>
    /// Initializes the middleware.
    /// </summary>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Adds a correlation identifier to the request, response, logging scope and current activity.
    /// Client-supplied identifiers are deliberately ignored.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId;
        context.Response.Headers[ResponseHeaderName] = correlationId;

        Activity.Current?.SetTag(ActivityTagName, correlationId);
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
