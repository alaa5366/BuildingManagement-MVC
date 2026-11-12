namespace BuildingManagementMvc.Models;

public class DvrDoc
{
    public string Id { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string Name { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public int Port { get; set; } = 554;
    public string Brand { get; set; } = "";
    public string Username { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = "";
}