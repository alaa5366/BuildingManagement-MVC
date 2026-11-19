namespace BuildingManagementMvc.Models;

/// <summary>Target-schema building summary used by DVR management screens.</summary>
public sealed class DvrBuildingSummary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string BuildingNumber { get; set; } = "";
    public int ApartmentsCount { get; set; }
}
