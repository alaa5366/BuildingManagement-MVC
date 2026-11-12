namespace BuildingManagementMvc.Models;

public class CameraDoc
{
    public string Id { get; set; } = "";
    public string DvrId { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Channel { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = "";
}