namespace BuildingManagementMvc.Models;

public class QrUsageDoc
{
    public string Id { get; set; } = "";
    public string TokenId { get; set; } = "";
    public int UseCount { get; set; }
    public string FirstUsedAt { get; set; } = "";
    public string LastUsedAt { get; set; } = "";
    public string? DeviceFingerprint { get; set; }
}