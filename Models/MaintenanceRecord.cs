namespace BuildingManagementMvc.Models;

public class MaintenanceRecord
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Date { get; set; } = "";
    public double Cost { get; set; }
    public string Vendor { get; set; } = "";
    public string Status { get; set; } = "open";
    public string CreatedBy { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public bool AddedToExpense { get; set; }
    public string? AddedToExpenseAt { get; set; }
}