namespace BuildingManagementMvc.Models;

/// <summary>
/// A maintenance record with cost, supplier and status.
/// </summary>
public class MaintenanceRecord
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = "";
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = "";
    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    public string Date { get; set; } = "";
    /// <summary>
    /// Gets or sets the cost.
    /// </summary>
    public decimal Cost { get; set; }
    /// <summary>
    /// Gets or sets the vendor.
    /// </summary>
    public string Vendor { get; set; } = "";
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "open";
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string CreatedBy { get; set; } = "";
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is added to expense.
    /// </summary>
    public bool AddedToExpense { get; set; }
    /// <summary>
    /// Gets or sets the added to expense date and time.
    /// </summary>
    public string? AddedToExpenseAt { get; set; }
}