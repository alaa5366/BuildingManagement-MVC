using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// خدمة إدارة الفئات المالية (مصروفات + إيرادات) داخل مستند العمارة
public class CategoriesService
{
    private readonly BuildingsService _buildings;
    private readonly IAuditLogger _audit;

    public CategoriesService(BuildingsService buildings, IAuditLogger audit)
    {
        _buildings = buildings;
        _audit = audit;
    }

    // ============================================================
    // قراءة
    // ============================================================
    public List<FinancialCategory> GetExpenseCategories(Building building)
        => (building.ExpenseCategories ?? new()).OrderBy(c => c.Order).ThenBy(c => c.Name).ToList();

    public List<FinancialCategory> GetRevenueCategories(Building building)
        => (building.RevenueCategories ?? new()).OrderBy(c => c.Order).ThenBy(c => c.Name).ToList();

    public FinancialCategory? FindExpenseCategory(Building building, string id)
        => building.ExpenseCategories?.FirstOrDefault(c => c.Id == id);

    public FinancialCategory? FindRevenueCategory(Building building, string id)
        => building.RevenueCategories?.FirstOrDefault(c => c.Id == id);

    // ============================================================
    // إنشاء
    // ============================================================
    public async Task<FinancialCategory> AddAsync(
        Building building,
        bool isExpense,
        string name,
        string color,
        string userId)
    {
        var list = isExpense ? building.ExpenseCategories : building.RevenueCategories;
        list ??= new List<FinancialCategory>();

        var id = Slugify(name);
        var baseId = id;
        var counter = 1;
        while (list.Any(c => c.Id == id))
        {
            id = $"{baseId}-{counter++}";
        }

        var cat = new FinancialCategory
        {
            Id = id,
            Name = name.Trim(),
            Color = NormalizeColor(color),
            Active = true,
            Order = list.Count == 0 ? 0 : list.Max(c => c.Order) + 1
        };

        list.Add(cat);
        if (isExpense) building.ExpenseCategories = list;
        else building.RevenueCategories = list;

        await _buildings.SaveFullAsync(building);

        await _audit.LogAsync(
            action: isExpense ? "category.expense.add" : "category.revenue.add",
            buildingId: building.Id,
            userId: userId,
            userRole: "admin",
            metadata: new { catId = cat.Id, cat.Name, cat.Color },
            severity: "info");

        return cat;
    }

    // ============================================================
    // تعديل
    // ============================================================
    public async Task<bool> UpdateAsync(
        Building building,
        bool isExpense,
        string id,
        string name,
        string color,
        bool active,
        string userId)
    {
        var list = isExpense ? building.ExpenseCategories : building.RevenueCategories;
        var cat = list?.FirstOrDefault(c => c.Id == id);
        if (cat == null) return false;

        var oldName = cat.Name;
        var oldColor = cat.Color;
        var oldActive = cat.Active;

        cat.Name = name.Trim();
        cat.Color = NormalizeColor(color);
        cat.Active = active;

        await _buildings.SaveFullAsync(building);

        await _audit.LogAsync(
            action: isExpense ? "category.expense.update" : "category.revenue.update",
            buildingId: building.Id,
            userId: userId,
            userRole: "admin",
            metadata: new
            {
                catId = id,
                old = new { oldName, oldColor, oldActive },
                @new = new { name, color, active }
            },
            severity: "warning");

        return true;
    }

    // ============================================================
    // حذف
    // ============================================================
    public async Task<bool> DeleteAsync(
        Building building,
        bool isExpense,
        string id,
        string userId)
    {
        var list = isExpense ? building.ExpenseCategories : building.RevenueCategories;
        var cat = list?.FirstOrDefault(c => c.Id == id);
        if (cat == null) return false;

        var usedCount = 0;
        foreach (var month in building.Months.Values)
        {
            if (isExpense)
                usedCount += month.Expenses.Count(e => e.CategoryId == id);
            else
                usedCount += month.Revenues.Count(r => r.CategoryId == id);
        }

        if (usedCount > 0)
        {
            cat.Active = false;
            await _buildings.SaveFullAsync(building);

            await _audit.LogAsync(
                action: isExpense ? "category.expense.deactivate" : "category.revenue.deactivate",
                buildingId: building.Id,
                userId: userId,
                userRole: "admin",
                metadata: new { catId = id, reason = "used", usedCount },
                severity: "warning");

            return true;
        }

        list!.Remove(cat);
        await _buildings.SaveFullAsync(building);

        await _audit.LogAsync(
            action: isExpense ? "category.expense.delete" : "category.revenue.delete",
            buildingId: building.Id,
            userId: userId,
            userRole: "admin",
            metadata: new { catId = id, name = cat.Name },
            severity: "critical");

        return true;
    }

    // ============================================================
    // إعادة الترتيب
    // ============================================================
    public async Task ReorderAsync(
        Building building,
        bool isExpense,
        List<string> orderedIds,
        string userId)
    {
        var list = isExpense ? building.ExpenseCategories : building.RevenueCategories;
        if (list == null) return;

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var cat = list.FirstOrDefault(c => c.Id == orderedIds[i]);
            if (cat != null) cat.Order = i;
        }

        await _buildings.SaveFullAsync(building);

        await _audit.LogAsync(
            action: isExpense ? "category.expense.reorder" : "category.revenue.reorder",
            buildingId: building.Id,
            userId: userId,
            userRole: "admin",
            metadata: new { count = orderedIds.Count },
            severity: "info");
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static string Slugify(string name)
    {
        var slug = name.Trim().ToLowerInvariant();
        var result = new System.Text.StringBuilder();
        foreach (var ch in slug)
        {
            if (char.IsLetterOrDigit(ch)) result.Append(ch);
            else if (result.Length > 0 && result[^1] != '-') result.Append('-');
        }
        var s = result.ToString().Trim('-');
        return string.IsNullOrEmpty(s) ? "cat-" + Guid.NewGuid().ToString("N")[..6] : s;
    }

    private static string NormalizeColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return "#888888";
        color = color.Trim();
        if (!color.StartsWith("#")) color = "#" + color;
        if (color.Length != 7) return "#888888";
        return color.ToUpperInvariant();
    }
}