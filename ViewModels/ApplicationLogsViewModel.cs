using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.ViewModels;

public sealed class ApplicationLogsViewModel
{
    public PagedResult<ApplicationLogEntry> Results { get; init; } = new();
    public ApplicationLogQuery Query { get; init; } = new();
}
