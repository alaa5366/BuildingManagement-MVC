namespace BuildingManagementMvc.Models;

/// <summary>
/// Configuration of scheduled backups (frequency, hour, retention, included collections).
/// </summary>
public class ScheduledBackupSettings
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "default";
    /// <summary>
    /// Gets or sets a value indicating whether it is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;
    /// <summary>
    /// Gets or sets the frequency.
    /// </summary>
    public string Frequency { get; set; } = "daily";
    /// <summary>
    /// Gets or sets the hour.
    /// </summary>
    public int Hour { get; set; } = 2;
    /// <summary>
    /// Gets or sets the day of week.
    /// </summary>
    public int? DayOfWeek { get; set; }
    /// <summary>
    /// Gets or sets the day of month.
    /// </summary>
    public int? DayOfMonth { get; set; }
    /// <summary>
    /// Gets or sets the collections.
    /// </summary>
    public List<string> Collections { get; set; } = new();
    /// <summary>
    /// Gets or sets the max backups to keep.
    /// </summary>
    public int MaxBackupsToKeep { get; set; } = 7;
    /// <summary>
    /// Gets or sets the last run date and time.
    /// </summary>
    public string? LastRunAt { get; set; }
    /// <summary>
    /// Gets or sets the last run status.
    /// </summary>
    public string? LastRunStatus { get; set; }
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public string? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// One entry of the backup history.
/// </summary>
public class BackupHistoryEntry
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = "";
    /// <summary>
    /// Gets or sets the file size.
    /// </summary>
    public long FileSize { get; set; }
    /// <summary>
    /// Gets or sets the collections.
    /// </summary>
    public List<string> Collections { get; set; } = new();
    /// <summary>
    /// Gets or sets the total documents.
    /// </summary>
    public int TotalDocuments { get; set; }
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public DateTime? CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the duration seconds.
    /// </summary>
    public double DurationSeconds { get; set; }
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "success";
    /// <summary>
    /// Gets or sets the source.
    /// </summary>
    public string Source { get; set; } = "scheduled";
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is file exists.
    /// </summary>
    public bool FileExists { get; set; }
}