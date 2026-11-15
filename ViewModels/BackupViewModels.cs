namespace BuildingManagementMvc.Models;

/// <summary>
/// Statistics shown on the backup overview page.
/// </summary>
public class BackupStats
{
    /// <summary>
    /// Gets or sets the collection.
    /// </summary>
    public string Collection { get; set; } = "";
    /// <summary>
    /// Gets or sets the label ar.
    /// </summary>
    public string LabelAr { get; set; } = "";
    /// <summary>
    /// Gets or sets the document count.
    /// </summary>
    public int DocCount { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is selected.
    /// </summary>
    public bool Selected { get; set; } = true;
}

/// <summary>
/// Result of creating a backup.
/// </summary>
public class BackupResult
{
    /// <summary>
    /// Gets or sets a value indicating whether it is success.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string? FileName { get; set; }
    /// <summary>
    /// Gets or sets the file size.
    /// </summary>
    public long FileSize { get; set; }
    /// <summary>
    /// Gets or sets the total collections.
    /// </summary>
    public int TotalCollections { get; set; }
    /// <summary>
    /// Gets or sets the total documents.
    /// </summary>
    public int TotalDocuments { get; set; }
    /// <summary>
    /// Gets or sets the collections.
    /// </summary>
    public List<string> Collections { get; set; } = new();
    /// <summary>
    /// Gets or sets the errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Preview of what a backup file contains before it is restored.
/// </summary>
public class RestorePreview
{
    /// <summary>
    /// Gets or sets a value indicating whether it is success.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string? FileName { get; set; }
    /// <summary>
    /// Gets or sets the file size.
    /// </summary>
    public long FileSize { get; set; }
    /// <summary>
    /// Gets or sets the collections.
    /// </summary>
    public List<RestoreCollectionPreview> Collections { get; set; } = new();
    /// <summary>
    /// Gets or sets the errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// Gets or sets the temp file path.
    /// </summary>
    public string? RestoreSessionId { get; set; }
    // Internal compatibility field for service-level tests; never rendered into the browser form.
    [System.Text.Json.Serialization.JsonIgnore]
    public string? TempFilePath { get; set; }
}

/// <summary>
/// Preview of one collection inside a backup file.
/// </summary>
public class RestoreCollectionPreview
{
    /// <summary>
    /// Gets or sets the collection.
    /// </summary>
    public string Collection { get; set; } = "";
    /// <summary>
    /// Gets or sets the label ar.
    /// </summary>
    public string LabelAr { get; set; } = "";
    /// <summary>
    /// Gets or sets the document count.
    /// </summary>
    public int DocCount { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is selected.
    /// </summary>
    public bool Selected { get; set; } = true;
    /// <summary>
    /// Gets or sets the size bytes.
    /// </summary>
    public long SizeBytes { get; set; }
}

/// <summary>
/// Result of restoring a backup.
/// </summary>
public class RestoreResult
{
    /// <summary>
    /// Gets or sets a value indicating whether it is success.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets the total collections.
    /// </summary>
    public int TotalCollections { get; set; }
    /// <summary>
    /// Gets or sets the total documents.
    /// </summary>
    public int TotalDocuments { get; set; }
    /// <summary>
    /// Gets or sets the restored.
    /// </summary>
    public int Restored { get; set; }
    /// <summary>
    /// Gets or sets the skipped.
    /// </summary>
    public int Skipped { get; set; }
    /// <summary>
    /// Gets or sets the failed.
    /// </summary>
    public int Failed { get; set; }
    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    public List<string> Details { get; set; } = new();
    /// <summary>
    /// Gets or sets the errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    public TimeSpan Duration { get; set; }
}