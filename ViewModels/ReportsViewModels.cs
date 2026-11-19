namespace BuildingManagementMvc.Models;

// نماذج عرض مخصّصة لشاشة التقارير (features/reports.js) — مش بتتخزن في Firestore
/// <summary>
/// Model of the reports page.
/// </summary>
public class ReportsVm
{
    /// <summary>
    /// Gets or sets the building.
    /// </summary>
    public Building Building { get; set; } = null!;
    /// <summary>
    /// Gets or sets the range.
    /// </summary>
    public string Range { get; set; } = "6m";

    /// <summary>
    /// Gets or sets the total collected.
    /// </summary>
    public decimal TotalCollected { get; set; }
    /// <summary>
    /// Gets or sets the total expenses.
    /// </summary>
    public decimal TotalExpenses { get; set; }
    /// <summary>
    /// Gets or sets the total revenues.
    /// </summary>
    public decimal TotalRevenues { get; set; }
    /// <summary>
    /// Gets or sets the net balance.
    /// </summary>
    public decimal NetBalance { get; set; }
    /// <summary>
    /// Gets or sets the avg monthly.
    /// </summary>
    public decimal AvgMonthly { get; set; }

    /// <summary>
    /// Gets or sets the monthly.
    /// </summary>
    public List<MonthlyPoint> Monthly { get; set; } = new();
    /// <summary>
    /// Gets or sets the categories.
    /// </summary>
    public List<CategoryPoint> Categories { get; set; } = new();
    /// <summary>
    /// Gets or sets the wallet.
    /// </summary>
    public List<WalletPoint> Wallet { get; set; } = new();
    /// <summary>
    /// Gets or sets the debtors.
    /// </summary>
    public List<TopEntry> Debtors { get; set; } = new();
    /// <summary>
    /// Gets or sets the creditors.
    /// </summary>
    public List<TopEntry> Creditors { get; set; } = new();
}

/// <summary>
/// One point of the monthly collected-versus-expenses series.
/// </summary>
public class MonthlyPoint
{
    /// <summary>
    /// Gets or sets the month.
    /// </summary>
    public string Month { get; set; } = "";
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the collected.
    /// </summary>
    public decimal Collected { get; set; }
    /// <summary>
    /// Gets or sets the revenues.
    /// </summary>
    public decimal Revenues { get; set; }
    /// <summary>
    /// Gets or sets the expenses.
    /// </summary>
    public decimal Expenses { get; set; }
}

/// <summary>
/// One slice of the expenses-by-category chart.
/// </summary>
public class CategoryPoint
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the color.
    /// </summary>
    public string Color { get; set; } = "";
    /// <summary>
    /// Gets or sets the total.
    /// </summary>
    public decimal Total { get; set; }
}

/// <summary>
/// One point of the wallet distribution chart.
/// </summary>
public class WalletPoint
{
    /// <summary>
    /// Gets or sets the apartment id.
    /// </summary>
    public string ApartmentId { get; set; } = "";
    /// <summary>
    /// Gets or sets the owner.
    /// </summary>
    public string Owner { get; set; } = "";
    /// <summary>
    /// Gets or sets the number.
    /// </summary>
    public int Number { get; set; }
    /// <summary>
    /// Gets or sets the label.
    /// </summary>
    public string Label { get; set; } = "";
    /// <summary>
    /// Gets or sets the balance.
    /// </summary>
    public decimal Balance { get; set; }
}

/// <summary>
/// One entry of the top debtors / top creditors lists.
/// </summary>
public class TopEntry
{
    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public Apartment Apt { get; set; } = null!;
    /// <summary>
    /// Gets or sets the balance.
    /// </summary>
    public decimal Balance { get; set; }
}
