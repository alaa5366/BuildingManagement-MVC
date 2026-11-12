namespace BuildingManagementMvc.Models;

public class Presence
{
    public string Uid { get; set; } = "";
    public string AptId { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string LastSeen { get; set; } = "";
    public long Ts { get; set; }
}