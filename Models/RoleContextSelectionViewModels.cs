namespace BuildingManagementMvc.Models;

/// <summary>Page model for choosing one verified role and authorization scope after phone/PIN authentication.</summary>
public sealed class RoleContextSelectionVm
{
    public string FullName { get; set; } = "";
    public string SessionToken { get; set; } = "";
    public List<RoleContextOptionVm> Contexts { get; set; } = new();
}

/// <summary>A display-safe role and scope choice; it contains no credential or secret material.</summary>
public sealed class RoleContextOptionVm
{
    public string Id { get; set; } = "";
    public string Role { get; set; } = "";
    public string GroupTitle { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
