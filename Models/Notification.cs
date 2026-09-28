using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// نفس شكل عنصر building.notifications[] في Firestore (core/notifications.js)
[FirestoreData]
public class Notification
{
    [FirestoreProperty("id")] public string Id { get; set; } = "";
    [FirestoreProperty("ts")] public string Ts { get; set; } = "";
    // admin | resident
    [FirestoreProperty("recipientType")] public string RecipientType { get; set; } = "";
    // لو resident: aptId
    [FirestoreProperty("recipientId")] public string? RecipientId { get; set; }
    [FirestoreProperty("type")] public string Type { get; set; } = "info";
    [FirestoreProperty("icon")] public string Icon { get; set; } = "🔔";
    [FirestoreProperty("title")] public string Title { get; set; } = "";
    [FirestoreProperty("body")] public string Body { get; set; } = "";
    [FirestoreProperty("read")] public bool Read { get; set; }
    [FirestoreProperty("readAt")] public string? ReadAt { get; set; }
}
