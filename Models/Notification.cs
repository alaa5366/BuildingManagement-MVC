namespace BuildingManagementMvc.Models;

public class Notification
{
    public string Id { get; set; } = "";
    public string Ts { get; set; } = "";
    public string RecipientType { get; set; } = "";
    public string? RecipientId { get; set; }
    public string Type { get; set; } = "info";
    public string Icon { get; set; } = "🔔";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool Read { get; set; }
    public string? ReadAt { get; set; }
}