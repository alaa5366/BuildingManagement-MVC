// =====================================================================
//  SyncReport - نتيجة المزامنة الكاملة (Firestore -> SQL)
// =====================================================================
namespace BuildingManagementMvc.Data;

public class SyncReport
{
    public int Users { get; set; }
    public int Buildings { get; set; }
    public TimeSpan Elapsed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}