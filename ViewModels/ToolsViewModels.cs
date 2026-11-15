namespace BuildingManagementMvc.Models;

// ============================================================
// Migration
// ============================================================
/// <summary>
/// Result of a data migration run shown on the tools page.
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Gets or sets the buildings scanned.
    /// </summary>
    public int BuildingsScanned { get; set; }
    /// <summary>
    /// Gets or sets the entries found.
    /// </summary>
    public int EntriesFound { get; set; }
    /// <summary>
    /// Gets or sets the entries migrated.
    /// </summary>
    public int EntriesMigrated { get; set; }
    /// <summary>
    /// Gets or sets the entries skipped.
    /// </summary>
    public int EntriesSkipped { get; set; }
    /// <summary>
    /// Gets or sets the entries failed.
    /// </summary>
    public int EntriesFailed { get; set; }
    /// <summary>
    /// Gets or sets the errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    public List<string> Details { get; set; } = new();
    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

// ============================================================
// Maintenance
// ============================================================
/// <summary>
/// Result of a database maintenance run shown on the tools page.
/// </summary>
public class MaintenanceResult
{
    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    public string ToolName { get; set; } = "";
    /// <summary>
    /// Gets or sets the items found.
    /// </summary>
    public int ItemsFound { get; set; }
    /// <summary>
    /// Gets or sets the items fixed.
    /// </summary>
    public int ItemsFixed { get; set; }
    /// <summary>
    /// Gets or sets the details.
    /// </summary>
    public List<string> Details { get; set; } = new();
}

// ============================================================
// Sync
// ============================================================
/// <summary>
/// One difference found by a synchronization check.
/// </summary>
public class SyncDiffItem
{
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string Type { get; set; } = "";    // "admin" | "resident" | "building"
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the issue.
    /// </summary>
    public string Issue { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is can auto fix.
    /// </summary>
    public bool CanAutoFix { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is fixed.
    /// </summary>
    public bool Fixed { get; set; }
}

/// <summary>
/// Result of a synchronization run.
/// </summary>
public class SyncResult
{
    /// <summary>
    /// Gets or sets the total checked.
    /// </summary>
    public int TotalChecked { get; set; }
    /// <summary>
    /// Gets or sets the total issues.
    /// </summary>
    public int TotalIssues { get; set; }
    /// <summary>
    /// Gets or sets the total fixed.
    /// </summary>
    public int TotalFixed { get; set; }
    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    public List<SyncDiffItem> Items { get; set; } = new();
    /// <summary>
    /// Gets or sets the duration.
    /// </summary>
    public TimeSpan Duration { get; set; }
}