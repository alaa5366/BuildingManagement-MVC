// =====================================================================
//  SyncReport - نتيجة المزامنة الكاملة (Firestore -> SQL)
// =====================================================================
namespace BuildingManagementMvc.Data;

/// <summary>
/// Summary of one data synchronization run.
/// </summary>
public class SyncReport
{
    /// <summary>
    /// Gets or sets the users.
    /// </summary>
    public int Users { get; set; }
    /// <summary>
    /// Gets or sets the buildings.
    /// </summary>
    public int Buildings { get; set; }
    /// <summary>
    /// Gets or sets the elapsed.
    /// </summary>
    public TimeSpan Elapsed { get; set; }
    /// <summary>
    /// Gets or sets the errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
    /// <summary>
    /// Gets or sets the warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
