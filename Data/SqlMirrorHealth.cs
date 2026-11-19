// =====================================================================
//  SqlMirrorHealth - عدّاد بسيط لحالة النسخ على SQL
//  بيستخدمه StorageSyncController لعرض الصحة في الصفحة.
// =====================================================================
namespace BuildingManagementMvc.Data;

/// <summary>
/// Thread-safe counters describing the health of the SQL persistence layer, shown on the maintenance screens.
/// </summary>
public static class SqlMirrorHealth
{
    private static int _failureCount;
    private static string? _lastError;
    private static DateTime? _lastErrorAt;
    private static DateTime? _lastSuccessAt;
    private static readonly object _lock = new();

    public static int FailureCount
    {
        get { lock (_lock) return _failureCount; }
    }

    public static string? LastError
    {
        get { lock (_lock) return _lastError; }
    }

    public static DateTime? LastErrorAt
    {
        get { lock (_lock) return _lastErrorAt; }
    }

    public static DateTime? LastSuccessAt
    {
        get { lock (_lock) return _lastSuccessAt; }
    }

    /// <summary>
    /// Records the failure.
    /// </summary>
    public static void RecordFailure(string context, Exception ex)
    {
        lock (_lock)
        {
            _failureCount++;
            _lastError = $"[{context}] {ex.Message}";
            _lastErrorAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Records the success.
    /// </summary>
    public static void RecordSuccess()
    {
        lock (_lock)
        {
            _lastSuccessAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Resets the operation on the SQL mirror health.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _lastError = null;
            _lastErrorAt = null;
        }
    }
}
