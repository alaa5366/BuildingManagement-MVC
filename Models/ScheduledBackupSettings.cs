namespace BuildingManagementMvc.Models;

public class ScheduledBackupSettings
{
    public string Id { get; set; } = "default";
    public bool Enabled { get; set; } = false;
    public string Frequency { get; set; } = "daily";
    public int Hour { get; set; } = 2;
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public List<string> Collections { get; set; } = new();
    public int MaxBackupsToKeep { get; set; } = 7;
    public string? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
    public string? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class BackupHistoryEntry
{
    public string Id { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public List<string> Collections { get; set; } = new();
    public int TotalDocuments { get; set; }
    public DateTime? CreatedAt { get; set; }
    public double DurationSeconds { get; set; }
    public string Status { get; set; } = "success";
    public string Source { get; set; } = "scheduled";
    public string? ErrorMessage { get; set; }
    public bool FileExists { get; set; }
}