using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/features/maintenance.js
/// <summary>
/// Business logic for maintenance records.
/// </summary>
public class MaintenanceService
{
    /// <summary>
    /// Gets the log.
    /// </summary>
    public List<MaintenanceRecord> GetLog(Building building) =>
        building.MaintenanceLog.OrderByDescending(m => m.Date).ToList();

    /// <summary>
    /// Adds the operation on the maintenance.
    /// </summary>
    public MaintenanceRecord Add(Building building, string title, string? description, string date, decimal cost, string? vendor, string status, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Maintenance title is required.", nameof(title));
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost), "Maintenance cost cannot be negative.");
        if (!DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out _))
            throw new ArgumentException("Maintenance date is invalid.", nameof(date));
        status = string.IsNullOrWhiteSpace(status) ? "open" : status.Trim().ToLowerInvariant();
        if (!new[] { "open", "inprogress", "done", "cancelled" }.Contains(status))
            throw new ArgumentException("Maintenance status is invalid.", nameof(status));
        var record = new MaintenanceRecord
        {
            Id = "mnt-" + Guid.NewGuid().ToString("N"),
            Title = title,
            Description = description ?? "",
            Date = date,
            Cost = cost,
            Vendor = vendor ?? "",
            Status = string.IsNullOrWhiteSpace(status) ? "open" : status,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            AddedToExpense = false
        };
        building.MaintenanceLog.Add(record);
        return record;
    }

    /// <summary>
    /// Updates the operation on the maintenance.
    /// </summary>
    public MaintenanceRecord? Update(Building building, string id, string title, string? description, string date, decimal cost, string? vendor, string status)
    {
        var record = building.MaintenanceLog.FirstOrDefault(r => r.Id == id);
        if (record == null) return null;
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Maintenance title is required.", nameof(title));
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost), "Maintenance cost cannot be negative.");
        if (!DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out _))
            throw new ArgumentException("Maintenance date is invalid.", nameof(date));
        status = (status ?? string.Empty).Trim().ToLowerInvariant();
        if (!new[] { "open", "inprogress", "done", "cancelled" }.Contains(status))
            throw new ArgumentException("Maintenance status is invalid.", nameof(status));
        record.Title = title;
        record.Description = description ?? "";
        record.Date = date;
        record.Cost = cost;
        record.Vendor = vendor ?? "";
        record.Status = status;
        return record;
    }

    /// <summary>
    /// Deletes the operation on the maintenance.
    /// </summary>
    public bool Delete(Building building, string id)
    {
        var before = building.MaintenanceLog.Count;
        building.MaintenanceLog = building.MaintenanceLog.Where(r => r.Id != id).ToList();
        return before != building.MaintenanceLog.Count;
    }
}
