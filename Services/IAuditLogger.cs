using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Services;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        string? userRole = null,
        object? metadata = null,
        string severity = "info");

    Task<List<AuditEntryDoc>> QueryAsync(
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? action = null,
        string? severity = null,
        int limit = 100);
}

[FirestoreData]
public class AuditEntryDoc
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "";

    [FirestoreProperty("action")]
    public string Action { get; set; } = "";

    [FirestoreProperty("buildingId")]
    public string? BuildingId { get; set; }

    [FirestoreProperty("apartmentId")]
    public string? ApartmentId { get; set; }

    [FirestoreProperty("userId")]
    public string? UserId { get; set; }

    [FirestoreProperty("userRole")]
    public string? UserRole { get; set; }

    [FirestoreProperty("severity")]
    public string Severity { get; set; } = "info";

    [FirestoreProperty("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [FirestoreProperty("createdAt")]
    public Google.Cloud.Firestore.Timestamp? CreatedAt { get; set; }

    [FirestoreProperty("timestamp")]
    public Google.Cloud.Firestore.Timestamp? Timestamp { get; set; }

    [FirestoreProperty("details")]
    public string? Details { get; set; }

    // ✅ Helper: تاريخ موحّد (مع حماية من DateTime.MinValue)
    public DateTime EffectiveDate
    {
        get
        {
            var dt = CreatedAt?.ToDateTime()
                  ?? Timestamp?.ToDateTime()
                  ?? DateTime.MinValue;

            // لو رجع MinValue، نرجع 1970 (Unix Epoch)
            if (dt == DateTime.MinValue)
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // نتأكد إنه UTC
            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

            return dt;
        }
    }
}