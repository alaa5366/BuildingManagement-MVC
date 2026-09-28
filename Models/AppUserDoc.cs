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

    // ✅ جديد — للمرحلة 15
    [FirestoreProperty("permissions")]
    public List<string> Permissions { get; set; } = new();

    [FirestoreProperty("isActive")]
    public bool IsActive { get; set; } = true;

    [FirestoreProperty("lastLoginAt")]
    public string? LastLoginAt { get; set; }

    [FirestoreProperty("createdAt")]
    public string? CreatedAt { get; set; }

    [FirestoreProperty("createdBy")]
    public string? CreatedBy { get; set; }

    [FirestoreProperty("updatedAt")]
    public string? UpdatedAt { get; set; }

    [FirestoreProperty("updatedBy")]
    public string? UpdatedBy { get; set; }
}