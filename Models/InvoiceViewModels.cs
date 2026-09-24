namespace BuildingManagementMvc.Models;

// بيانات فاتورة المحفظة (features/invoice.js) — بتتحسب وقت الطلب، مش بتتخزن
public class InvoiceData
{
    public string InvoiceNo { get; set; } = "";
    public string IssueDate { get; set; } = "";
    public Building Building { get; set; } = null!;
    public Apartment Apt { get; set; } = null!;
    public Floor? Floor { get; set; }

    public double Balance { get; set; }
    public double PreviousBalance { get; set; }
    public double MonthDeposits { get; set; }
    public double MonthExpensesShare { get; set; }
    public double MonthRevenuesShare { get; set; }

    public List<CategoryShare> CategoryShares { get; set; } = new();
    public PaymentInfo Payment { get; set; } = new();

    public string MonthKey { get; set; } = "";
    public string MonthLabelText { get; set; } = "";
    public string QrPayload { get; set; } = "";
}

public class CategoryShare
{
    public string Name { get; set; } = "";
    public string ColorHex { get; set; } = "#888888";
    public double Total { get; set; }
    public int AptCount { get; set; }
    public double Share { get; set; }
}
