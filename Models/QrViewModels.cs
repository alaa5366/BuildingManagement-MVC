namespace BuildingManagementMvc.Models;

public sealed class QrApartmentInfo
{
    public string BuildingId { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public string BuildingNumber { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public int ApartmentNumber { get; set; }
    public string Owner { get; set; } = "";
    public string Phone { get; set; } = "";
    public string FloorLabel { get; set; } = "";
    public int FloorOrder { get; set; }
}

public sealed class QrBuildingPageModel
{
    public string BuildingName { get; set; } = "";
    public string BuildingNumber { get; set; } = "";
    public List<QrApartmentInfo> Apartments { get; set; } = new();
}
