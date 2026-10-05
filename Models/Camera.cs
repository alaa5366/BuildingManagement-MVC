using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

[FirestoreData]
public class CameraDoc
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "";

    [FirestoreProperty("dvrId")]
    public string DvrId { get; set; } = "";

    [FirestoreProperty("buildingId")]
    public string BuildingId { get; set; } = "";

    [FirestoreProperty("name")]
    public string Name { get; set; } = "";

    [FirestoreProperty("channel")]
    public int Channel { get; set; }

    [FirestoreProperty("isActive")]
    public bool IsActive { get; set; } = true;

    [FirestoreProperty("createdAt")]
    public string CreatedAt { get; set; } = "";
}
