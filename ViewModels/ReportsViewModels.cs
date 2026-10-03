namespace BuildingManagementMvc.Models;

// نماذج عرض مخصّصة لشاشة التقارير (features/reports.js) — مش بتتخزن في Firestore
public class ReportsVm
{
    public Building Building { get; set; } = null!;
    public string Range { get; set; } = "6m";

    public double TotalCollected { get; set; }
    public double TotalExpenses { get; set; }
    public double TotalRevenues { get; set; }
    public double NetBalance { get; set; }
    public double AvgMonthly { get; set; }

    public List<MonthlyPoint> Monthly { get; set; } = new();
    public List<CategoryPoint> Categories { get; set; } = new();
    public List<WalletPoint> Wallet { get; set; } = new();
    public List<TopEntry> Debtors { get; set; } = new();
    public List<TopEntry> Creditors { get; set; } = new();
}

public class MonthlyPoint
{
    public string Month { get; set; } = "";
    public string Label { get; set; } = "";
    public double Collected { get; set; }
    public double Revenues { get; set; }
    public double Expenses { get; set; }
}

public class CategoryPoint
{
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public double Total { get; set; }
}

public class WalletPoint
{
    public int Number { get; set; }
    public string Label { get; set; } = "";
    public double Balance { get; set; }
}

public class TopEntry
{
    public Apartment Apt { get; set; } = null!;
    public double Balance { get; set; }
}
