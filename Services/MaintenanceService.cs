using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/features/maintenance.js
public class MaintenanceService
{
    private readonly WalletService _wallet;
    public MaintenanceService(WalletService wallet) => _wallet = wallet;

    public List<MaintenanceRecord> GetLog(Building building) =>
        building.MaintenanceLog.OrderByDescending(m => m.Date).ToList();

    public MaintenanceRecord Add(Building building, string title, string? description, string date, double cost, string? vendor, string status, string createdBy)
    {
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

    public MaintenanceRecord? Update(Building building, string id, string title, string? description, string date, double cost, string? vendor, string status)
    {
        var record = building.MaintenanceLog.FirstOrDefault(r => r.Id == id);
        if (record == null) return null;
        record.Title = title;
        record.Description = description ?? "";
        record.Date = date;
        record.Cost = cost;
        record.Vendor = vendor ?? "";
        record.Status = status;
        return record;
    }

    public bool Delete(Building building, string id)
    {
        var before = building.MaintenanceLog.Count;
        building.MaintenanceLog = building.MaintenanceLog.Where(r => r.Id != id).ToList();
        return before != building.MaintenanceLog.Count;
    }

    // إضافة تكلفة الصيانة كمصروف على الشهر الحالي وإعادة حساب التوزيع
    public Expense? AddCostToExpenses(Building building, string id, string monthKey, string createdBy)
    {
        var record = building.MaintenanceLog.FirstOrDefault(r => r.Id == id);
        if (record == null || record.AddedToExpense) return null;

        var m = _wallet.GetOrCreateMonth(building, monthKey);

        var cat = building.ExpenseCategories.FirstOrDefault(c => c.Name.Contains("صيانة") || c.Name.ToLower().Contains("maintenance"))
            ?? building.ExpenseCategories.FirstOrDefault(c => c.Id == "other")
            ?? building.ExpenseCategories.FirstOrDefault();

        var expense = new Expense
        {
            Id = Guid.NewGuid().ToString("N"),
            CategoryId = cat?.Id ?? "other",
            Note = "🔧 " + record.Title + (string.IsNullOrWhiteSpace(record.Vendor) ? "" : $" — {record.Vendor}"),
            Amount = record.Cost,
            Date = string.IsNullOrWhiteSpace(record.Date) ? DateTime.UtcNow.ToString("yyyy-MM-dd") : record.Date
        };
        m.Expenses.Add(expense);

        record.AddedToExpense = true;
        record.AddedToExpenseAt = DateTime.UtcNow.ToString("o");

        _wallet.RecalculateDistribution(building, monthKey);

        return expense;
    }
}
