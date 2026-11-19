namespace BuildingManagementMvc.Models;

/// <summary>Defines the filters and paging parameters for an application log search.</summary>
public sealed class ApplicationLogQuery
{
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public string? Level { get; set; }
    public string? Search { get; set; }
    public string? CorrelationId { get; set; }
    public string? Source { get; set; }
    public string? ComponentType { get; set; }
    public bool? HasException { get; set; }
    public int? HttpStatusCode { get; set; }
    public double? MinDurationMs { get; set; }
    public double? MaxDurationMs { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
