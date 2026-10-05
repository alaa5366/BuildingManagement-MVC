using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

[FirestoreData]
public class DvrDoc
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "";

    [FirestoreProperty("buildingId")]
    public string BuildingId { get; set; } = "";

    [FirestoreProperty("name")]
    public string Name { get; set; } = "";

    [FirestoreProperty("ip")]
    public string IpAddress { get; set; } = "";

    [FirestoreProperty("port")]
    public int Port { get; set; } = 554;

    [FirestoreProperty("brand")]
    public string Brand { get; set; } = "";

    [FirestoreProperty("username")]
    public string Username { get; set; } = "";

    [FirestoreProperty("isActive")]
    public bool IsActive { get; set; } = true;

    [FirestoreProperty("createdAt")]
    public string CreatedAt { get; set; } = "";

    // ⚠️ مفيش PasswordEncrypted هنا (SQL فقط)
}