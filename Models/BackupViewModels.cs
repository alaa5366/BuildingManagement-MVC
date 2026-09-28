namespace BuildingManagementMvc.Models;

public class BackupStats
{
    public string Collection { get; set; } = "";
    public string LabelAr { get; set; } = "";
    public int DocCount { get; set; }
    public bool Selected { get; set; } = true;
}

public class BackupResult
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public int TotalCollections { get; set; }
    public int TotalDocuments { get; set; }
    public List<string> Collections { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class RestorePreview
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public List<RestoreCollectionPreview> Collections { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string? TempFilePath { get; set; }
}

public class RestoreCollectionPreview
{
    public string Collection { get; set; } = "";
    public string LabelAr { get; set; } = "";
    public int DocCount { get; set; }
    public bool Selected { get; set; } = true;
    public long SizeBytes { get; set; }
}

public class RestoreResult
{
    public bool Success { get; set; }
    public int TotalCollections { get; set; }
    public int TotalDocuments { get; set; }
    public int Restored { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> Details { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}