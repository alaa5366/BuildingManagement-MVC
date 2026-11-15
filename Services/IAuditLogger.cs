namespace BuildingManagementMvc.Services;

/// <summary>
/// Writes audit entries (who did what to which building/apartment) to persistent storage.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Logs the operation on the i audit logger.
    /// </summary>
    Task LogAsync(
        string action,
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        string? userRole = null,
        object? metadata = null,
        string severity = "info");

    /// <summary>
    /// Returns the query.
    /// </summary>
    Task<List<AuditEntryDoc>> QueryAsync(
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? action = null,
        string? severity = null,
        int limit = 100);

    Task<BuildingManagementMvc.Models.PagedResult<AuditEntryDoc>> QueryPageAsync(
        string? buildingId, string? apartmentId, string? userId, DateTime? from, DateTime? to,
        string? action, string? severity, int page, int pageSize);
}

/// <summary>
/// One audit entry as passed to <see cref="IAuditLogger"/>.
/// </summary>
public class AuditEntryDoc
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the action.
    /// </summary>
    public string Action { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string? BuildingId { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string? ApartmentId { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user.
    /// </summary>
    public string? UserId { get; set; }
    /// <summary>
    /// Gets or sets the user role.
    /// </summary>
    public string? UserRole { get; set; }
    /// <summary>
    /// Gets or sets the severity.
    /// </summary>
    public string Severity { get; set; } = "info";
    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime? CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime? Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    public string? Details { get; set; }

    public DateTime EffectiveDate
    {
        get
        {
            var dt = CreatedAt
                  ?? Timestamp
                  ?? DateTime.MinValue;

            if (dt == DateTime.MinValue)
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            return dt;
        }
    }
}