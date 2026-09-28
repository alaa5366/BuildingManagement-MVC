using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

[FirestoreData]
public class ScheduledBackupSettings
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "default";

    [FirestoreProperty("enabled")]
    public bool Enabled { get; set; } = false;

    [FirestoreProperty("frequency")]
    public string Frequency { get; set; } = "daily";

    [FirestoreProperty("hour")]
    public int Hour { get; set; } = 2;

    [FirestoreProperty("dayOfWeek")]
    public int? DayOfWeek { get; set; }

    [FirestoreProperty("dayOfMonth")]
    public int? DayOfMonth { get; set; }

    [FirestoreProperty("collections")]
    public List<string> Collections { get; set; } = new();

    [FirestoreProperty("maxBackupsToKeep")]
    public int MaxBackupsToKeep { get; set; } = 7;

    [FirestoreProperty("lastRunAt")]
    public string? LastRunAt { get; set; }

    [FirestoreProperty("lastRunStatus")]
    public string? LastRunStatus { get; set; }

    [FirestoreProperty("updatedAt")]
    public string? UpdatedAt { get; set; }

    [FirestoreProperty("updatedBy")]
    public string? UpdatedBy { get; set; }
}

[FirestoreData]
public class BackupHistoryEntry
{
    [FirestoreDocumentId]
    public string Id { get; set; } = "";

    [FirestoreProperty("fileName")]
    public string FileName { get; set; } = "";

    [FirestoreProperty("fileSize")]
    public long FileSize { get; set; }

    [FirestoreProperty("collections")]
    public List<string> Collections { get; set; } = new();

    [FirestoreProperty("totalDocuments")]
    public int TotalDocuments { get; set; }

    [FirestoreProperty("createdAt")]
    public Google.Cloud.Firestore.Timestamp? CreatedAt { get; set; }

    [FirestoreProperty("durationSeconds")]
    public double DurationSeconds { get; set; }

    [FirestoreProperty("status")]
    public string Status { get; set; } = "success";

    [FirestoreProperty("source")]
    public string Source { get; set; } = "scheduled";

    [FirestoreProperty("errorMessage")]
    public string? ErrorMessage { get; set; }

    // ✅ حقل runtime (مش بيتخزن في Firestore — الـ SDK بتتجاهله لأن مفيش FirestoreProperty)
    public bool FileExists { get; set; }
}