namespace BuildingManagementMvc.Models;

public sealed class ApplicationLogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Level { get; init; } = "";
    public string Message { get; init; } = "";
    public string SourceContext { get; init; } = "";
    public string CorrelationId { get; init; } = "";
    public string Exception { get; set; } = "";
    public string FileName { get; init; } = "";
    public bool HasException => !string.IsNullOrWhiteSpace(Exception);
}
