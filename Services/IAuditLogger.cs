namespace BuildingManagementMvc.Services;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        string? userRole = null,
        object? metadata = null,
        string severity = "info");

    Task<List<AuditEntryDoc>> QueryAsync(
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? action = null,
        string? severity = null,
        int limit = 100);
}

public class AuditEntryDoc
{
    public string Id { get; set; } = "";
    public string Action { get; set; } = "";
    public string? BuildingId { get; set; }
    public string? ApartmentId { get; set; }
    public string? UserId { get; set; }
    public string? UserRole { get; set; }
    public string Severity { get; set; } = "info";
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? Timestamp { get; set; }
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