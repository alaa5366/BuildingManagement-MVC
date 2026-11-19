namespace BuildingManagementMvc.Models;

/// <summary>Safe summary of the current process and registered health checks.</summary>
public sealed class SystemHealthViewModel
{
    public DateTimeOffset CheckedAtUtc { get; init; }
    public DateTimeOffset ProcessStartedAtUtc { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public string OverallStatus { get; init; } = "Unknown";
    public long WorkingSetBytes { get; init; }
    public long PrivateMemoryBytes { get; init; }
    public long ManagedMemoryBytes { get; init; }
    public int ThreadCount { get; init; }
    public int HandleCount { get; init; }
    public IReadOnlyList<SystemHealthCheckViewModel> Checks { get; init; } = Array.Empty<SystemHealthCheckViewModel>();
}

/// <summary>Safe, non-sensitive health check result for display.</summary>
public sealed class SystemHealthCheckViewModel
{
    public string Name { get; init; } = "";
    public string Status { get; init; } = "Unknown";
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
