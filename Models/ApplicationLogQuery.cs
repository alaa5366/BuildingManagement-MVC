namespace BuildingManagementMvc.Models;

public sealed class ApplicationLogQuery
{
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public string? Level { get; set; }
    public string? Search { get; set; }
    public string? CorrelationId { get; set; }
    public string? Source { get; set; }
    public bool? HasException { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
