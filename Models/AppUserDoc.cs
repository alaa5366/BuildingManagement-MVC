namespace BuildingManagementMvc.Models;

public class AppUserDoc
{
    public string Uid { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Whatsapp { get; set; } = "";
    public string Pin { get; set; } = "";
    public string PhotoUrl { get; set; } = "";
    public string Role { get; set; } = "";
    public List<string> BuildingIds { get; set; } = new();
    public bool Disabled { get; set; }
    public string DisabledReason { get; set; } = "";
    public List<string> Permissions { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public string? LastLoginAt { get; set; }
    public string? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}