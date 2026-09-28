using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// نفس شكل عنصر building.maintenanceLog[] في Firestore (features/maintenance.js)
[FirestoreData]
public class MaintenanceRecord
{
    [FirestoreProperty("id")] public string Id { get; set; } = "";
    [FirestoreProperty("title")] public string Title { get; set; } = "";
    [FirestoreProperty("description")] public string Description { get; set; } = "";
    [FirestoreProperty("date")] public string Date { get; set; } = "";
    [FirestoreProperty("cost")] public double Cost { get; set; }
    [FirestoreProperty("vendor")] public string Vendor { get; set; } = "";
    // open | inprogress | done | cancelled
    [FirestoreProperty("status")] public string Status { get; set; } = "open";
    [FirestoreProperty("createdBy")] public string CreatedBy { get; set; } = "";
    [FirestoreProperty("createdAt")] public string CreatedAt { get; set; } = "";
    [FirestoreProperty("addedToExpense")] public bool AddedToExpense { get; set; }
    [FirestoreProperty("addedToExpenseAt")] public string? AddedToExpenseAt { get; set; }
}
