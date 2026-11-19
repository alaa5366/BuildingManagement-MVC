namespace BuildingManagementMvc.Models;

/// <summary>Represents one sanitized application log event displayed in the log explorer.</summary>
public sealed class ApplicationLogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Level { get; init; } = "";
    public string Message { get; init; } = "";
    public string SourceContext { get; init; } = "";
    public string CorrelationId { get; init; } = "";
    public string Exception { get; set; } = "";
    public string Details { get; set; } = "";
    public string FileName { get; init; } = "";
    public string HttpMethod { get; init; } = "";
    public string RequestPath { get; init; } = "";
    public int? HttpStatusCode { get; init; }
    public double? DurationMs { get; init; }
    public bool HasException => !string.IsNullOrWhiteSpace(Exception);

    /// <summary>
    /// Classifies only explicit source-context evidence. Missing or ambiguous source
    /// metadata is intentionally reported as Other rather than guessed.
    /// </summary>
    public string ComponentType
    {
        get
        {
            var source = SourceContext;
            if (string.IsNullOrWhiteSpace(source))
                return "Other";

            if (source.Contains("EntityFrameworkCore.Database.Command", StringComparison.OrdinalIgnoreCase) ||
                source.Contains(".Database.", StringComparison.OrdinalIgnoreCase))
                return "Database";

            if (source.Contains(".Middleware.", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Middleware", StringComparison.OrdinalIgnoreCase))
                return "Middleware";

            if (source.Contains(".Controllers.", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                return "Controller";

            if (source.Contains(".Services.", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
                return "Service";

            if (source.Contains("AspNetCoreGeneratedDocument.Views_", StringComparison.OrdinalIgnoreCase) ||
                source.Contains(".Views.", StringComparison.OrdinalIgnoreCase))
                return "View";

            if (source.Contains(".Repositories.", StringComparison.OrdinalIgnoreCase) ||
                source.Contains(".Data.", StringComparison.OrdinalIgnoreCase) ||
                source.Contains(".Stores.", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Repository", StringComparison.OrdinalIgnoreCase))
                return "Repository";

            if (source.EndsWith("HostedService", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("BackgroundService", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Scheduler", StringComparison.OrdinalIgnoreCase) ||
                source.EndsWith("Worker", StringComparison.OrdinalIgnoreCase))
                return "BackgroundJob";

            if (source.StartsWith("System.Net.Http.", StringComparison.OrdinalIgnoreCase) ||
                source.StartsWith("Microsoft.Extensions.Http.", StringComparison.OrdinalIgnoreCase))
                return "ExternalDependency";

            return "Other";
        }
    }

    /// <summary>Returns the source-context class/type name without its namespace.</summary>
    public string ComponentName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SourceContext))
                return "";
            var separator = SourceContext.LastIndexOf('.');
            return separator >= 0 ? SourceContext[(separator + 1)..] : SourceContext;
        }
    }
}
