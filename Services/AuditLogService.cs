using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// نسخة مبسطة من js/core/audit-log.js (من غير i18n keys)
/// <summary>
/// Helper that appends audit entries to a building aggregate.
/// </summary>
public class AuditLogService
{
    /// <summary>
    /// Pushes the operation on the audit log.
    /// </summary>
    public void Push(Building building, string action, string details, string actorRole, string actorName,
        string? aptNumber = null, string severity = "info")
    {
        building.AuditLog ??= new List<AuditLogEntry>();

        var severityValue = (severity ?? "info").ToLowerInvariant();
        var normalizedSeverity = new[] { "info", "warning", "error", "critical" }.Contains(severityValue)
            ? severityValue : "info";
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Ts = DateTime.UtcNow.ToString("o"),
            Action = action,
            Label = action,
            Actor = actorName,
            ActorRole = actorRole,
            Details = details,
            BuildingNumber = building.BuildingNumber,
            Severity = normalizedSeverity
        };

        if (int.TryParse(aptNumber, out var n)) entry.AptNumber = n;

        building.AuditLog.Add(entry);

        if (building.AuditLog.Count > 1000)
            building.AuditLog = building.AuditLog.Skip(building.AuditLog.Count - 1000).ToList();
    }
}
