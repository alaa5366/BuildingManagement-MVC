namespace BuildingManagementMvc.Models;

// ============================================================
// Migration
// ============================================================
public class MigrationResult
{
    public int BuildingsScanned { get; set; }
    public int EntriesFound { get; set; }
    public int EntriesMigrated { get; set; }
    public int EntriesSkipped { get; set; }
    public int EntriesFailed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Details { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

// ============================================================
// Maintenance
// ============================================================
public class MaintenanceResult
{
    public string ToolName { get; set; } = "";
    public int ItemsFound { get; set; }
    public int ItemsFixed { get; set; }
    public List<string> Details { get; set; } = new();
}

// ============================================================
// Sync
// ============================================================
public class SyncDiffItem
{
    public string Type { get; set; } = "";    // "admin" | "resident" | "building"
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Issue { get; set; } = "";
    public bool CanAutoFix { get; set; }
    public bool Fixed { get; set; }
}

public class SyncResult
{
    public int TotalChecked { get; set; }
    public int TotalIssues { get; set; }
    public int TotalFixed { get; set; }
    public List<SyncDiffItem> Items { get; set; } = new();
    public TimeSpan Duration { get; set; }
}