namespace BuildingManagementMvc.Models;

// بيانات فاتورة المحفظة (features/invoice.js) — بتتحسب وقت الطلب، مش بتتخزن
/// <summary>
/// All data needed to render an invoice.
/// </summary>
public class InvoiceData
{
    /// <summary>
    /// Gets or sets the invoice no.
    /// </summary>
    public string InvoiceNo { get; set; } = "";
    /// <summary>
    /// Gets or sets the issue date.
    /// </summary>
    public string IssueDate { get; set; } = "";
    /// <summary>
    /// Gets or sets the building.
    /// </summary>
    public Building Building { get; set; } = null!;
    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public Apartment Apt { get; set; } = null!;
    /// <summary>
    /// Gets or sets the floor.
    /// </summary>
    public Floor? Floor { get; set; }
    /// <summary>
    /// Gets or sets the ISO currency code used by the building.
    /// </summary>
    public string CurrencyCode { get; set; } = "EGP";
    /// <summary>
    /// Gets or sets the balance.
    /// </summary>
    public decimal Balance { get; set; }
    /// <summary>
    /// Gets or sets the previous balance.
    /// </summary>
    public decimal PreviousBalance { get; set; }
    /// <summary>
    /// Gets or sets the month deposits.
    /// </summary>
    public decimal MonthDeposits { get; set; }
    /// <summary>
    /// Gets or sets the month expenses share.
    /// </summary>
    public decimal MonthExpensesShare { get; set; }
    /// <summary>
    /// Gets or sets the month revenues share.
    /// </summary>
    public decimal MonthRevenuesShare { get; set; }
    // ✅ جديد
    /// <summary>
    /// Gets or sets the month fee.
    /// </summary>
    public decimal MonthFee { get; set; }
    /// <summary>
    /// Gets or sets the category shares.
    /// </summary>
    public List<CategoryShare> CategoryShares { get; set; } = new();
    /// <summary>
    /// Gets or sets the payment.
    /// </summary>
    public PaymentInfo Payment { get; set; } = new();
    /// <summary>
    /// Gets or sets the month key.
    /// </summary>
    public string MonthKey { get; set; } = "";
    /// <summary>
    /// Gets or sets the month label text.
    /// </summary>
    public string MonthLabelText { get; set; } = "";
    /// <summary>
    /// Gets or sets the QR payload.
    /// </summary>
    public string QrPayload { get; set; } = "";
}

/// <summary>
/// An apartment's share of one expense category on an invoice.
/// </summary>
public class CategoryShare
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the color hex.
    /// </summary>
    public string ColorHex { get; set; } = "#888888";
    /// <summary>
    /// Gets or sets the total.
    /// </summary>
    public decimal Total { get; set; }
    /// <summary>
    /// Gets or sets the apartment count.
    /// </summary>
    public int AptCount { get; set; }
    /// <summary>
    /// Gets or sets the share.
    /// </summary>
    public decimal Share { get; set; }
}
