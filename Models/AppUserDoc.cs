using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// نفس شكل مستند users/{uid} في Firestore
[FirestoreData]
public class AppUserDoc
{
    [FirestoreDocumentId]
    public string Uid { get; set; } = "";

    [FirestoreProperty("email")]
    public string Email { get; set; } = "";

    [FirestoreProperty("name")]
    public string Name { get; set; } = "";

    [FirestoreProperty("phone")]
    public string Phone { get; set; } = "";

    [FirestoreProperty("pin")]
    public string Pin { get; set; } = "";

    [FirestoreProperty("photoURL")]
    public string PhotoUrl { get; set; } = "";

    // superadmin | admin
    [FirestoreProperty("role")]
    public string Role { get; set; } = "";

    [FirestoreProperty("buildingIds")]
    public List<string> BuildingIds { get; set; } = new();

    [FirestoreProperty("disabled")]
    public bool Disabled { get; set; }

    [FirestoreProperty("disabledReason")]
    public string DisabledReason { get; set; } = "";
}
