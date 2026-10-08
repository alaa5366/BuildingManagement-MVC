using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// ✅ نفس شكل مستند qr_usage/{docId} في Firestore
[FirestoreData]
public class QrUsageDoc
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "";

    [FirestoreProperty("tokenId")]
    public string TokenId { get; set; } = "";

    [FirestoreProperty("useCount")]
    public int UseCount { get; set; }

    [FirestoreProperty("firstUsedAt")]
    public string FirstUsedAt { get; set; } = "";

    [FirestoreProperty("lastUsedAt")]
    public string LastUsedAt { get; set; } = "";

    [FirestoreProperty("deviceFingerprint")]
    public string? DeviceFingerprint { get; set; }
}