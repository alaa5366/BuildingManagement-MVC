using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.ViewModels;

/// <summary>Supplies the application log search criteria and paged results to the view.</summary>
public sealed class ApplicationLogsViewModel
{
    public PagedResult<ApplicationLogEntry> Results { get; init; } = new();
    public ApplicationLogQuery Query { get; init; } = new();
}
