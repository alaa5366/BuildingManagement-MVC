using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// ✅ نفس شكل مستند presence/{uid} في Firestore (متوافق مع JS)
[FirestoreData]
public class Presence
{
    [FirestoreDocumentId]
    public string Uid { get; set; } = "";

    [FirestoreProperty("aptId")]
    public string AptId { get; set; } = "";

    [FirestoreProperty("buildingId")]
    public string BuildingId { get; set; } = "";

    [FirestoreProperty("lastSeen")]
    public string LastSeen { get; set; } = "";

    [FirestoreProperty("ts")]
    public long Ts { get; set; }  // Unix milliseconds
}