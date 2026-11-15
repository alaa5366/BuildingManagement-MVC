using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class InvoiceServiceTests
{
    private readonly WalletService _wallet = new();
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _service = new InvoiceService(_wallet);
    }

    private static Building CreateBuildingWithApt()
    {
        var b = new Building
        {
            Name = "Test Tower",
            BuildingNumber = "BLD-001",
            PaymentInfo = new PaymentInfo { Label = "Instapay", AccountNumber = "123" }
        };
        b.Floors.Add(new Floor { Id = "f1", Label = "Floor 1", Order = 0 });
        b.Apartments.Add(new Apartment
        {
            Id = "a1",
            FloorId = "f1",
            Number = 1,
            Owner = "Ali",
            Phone = "0100",
            MonthlyFee = 100
        });
        return b;
    }

    // ================================================================
    // BuildInvoiceData — الأساسيات
    // ================================================================

    [Fact]
    public void BuildInvoiceData_ValidInput_ReturnsPopulatedData()
    {
        var b = CreateBuildingWithApt();
        var apt = b.Apartments[0];
        _wallet.EnsureMonth(b, "2025-01");

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        Assert.NotNull(invoice);
        Assert.Equal("BLD-001", invoice.Building.BuildingNumber);
        Assert.Equal(1, invoice.Apt.Number);
        Assert.Equal("2025-01", invoice.MonthKey);
        Assert.NotNull(invoice.InvoiceNo);
        Assert.Contains("INV-BLD-001-1-202501", invoice.InvoiceNo);
    }

    [Fact]
    public void BuildInvoiceData_InvoiceNoHasRandomSuffix()
    {
        var b = CreateBuildingWithApt();
        var apt = b.Apartments[0];
        _wallet.EnsureMonth(b, "2025-01");

        var inv1 = _service.BuildInvoiceData(b, apt, "2025-01");
        var inv2 = _service.BuildInvoiceData(b, apt, "2025-01");

        // عشان فيه random suffix، لازم يكونوا مختلفين
        Assert.NotEqual(inv1.InvoiceNo, inv2.InvoiceNo);
    }

    // ================================================================
    // BuildInvoiceData — الحسابات
    // ================================================================

    [Fact]
    public void BuildInvoiceData_CalculatesBalance()
    {
        var b = CreateBuildingWithApt();
        var apt = b.Apartments[0];
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" });

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        // 500 (deposit) - 100 (fee) = 400
        Assert.Equal(400, invoice.Balance);
    }

    [Fact]
    public void BuildInvoiceData_WithExpenses_CalculatesShares()
    {
        var b = CreateBuildingWithApt();
        b.ExpenseCategories.Add(new FinancialCategory { Id = "water", Name = "Water", Color = "#4A90D9" });
        var apt = b.Apartments[0];
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.Expenses.Add(new Expense { Id = "e1", CategoryId = "water", Amount = 300 });

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        Assert.NotEmpty(invoice.CategoryShares);
        var waterShare = invoice.CategoryShares.FirstOrDefault(c => c.Name == "Water");
        Assert.NotNull(waterShare);
        Assert.Equal(300, waterShare.Total);
        Assert.Equal(300, waterShare.Share); // شقة واحدة بس
    }

    [Fact]
    public void BuildInvoiceData_WithMultipleApartments_DividesExpenses()
    {
        var b = CreateBuildingWithApt();
        b.Apartments.Add(new Apartment { Id = "a2", FloorId = "f1", Number = 2 });
        b.ExpenseCategories.Add(new FinancialCategory { Id = "water", Name = "Water", Color = "#4A90D9" });
        var apt = b.Apartments[0];
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.Expenses.Add(new Expense { Id = "e1", CategoryId = "water", Amount = 300 });

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        var waterShare = invoice.CategoryShares.FirstOrDefault(c => c.Name == "Water");
        Assert.NotNull(waterShare);
        Assert.Equal(300, waterShare.Total);
        Assert.Equal(2, waterShare.AptCount);
        Assert.Equal(150, waterShare.Share); // 300 / 2
    }

    [Fact]
    public void BuildInvoiceData_WithCarryOver_SetsPreviousBalance()
    {
        var b = CreateBuildingWithApt();
        var apt = b.Apartments[0];
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.CarryOver["a1"] = 200;

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        Assert.Equal(200, invoice.PreviousBalance);
    }

    [Fact]
    public void BuildInvoiceData_WithRevenueDistribution_IncludesIt()
    {
        var b = CreateBuildingWithApt();
        var apt = b.Apartments[0];
        var m = _wallet.EnsureMonth(b, "2025-01");
        m.RevenueDistribution["a1"] = 150;

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        Assert.Equal(150, invoice.MonthRevenuesShare);
    }

    [Fact]
    public void BuildInvoiceData_WithMonthlyFee_IncludesIt()
    {
        var b = CreateBuildingWithApt();
        var apt = b.Apartments[0];
        _wallet.EnsureMonth(b, "2025-01");

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        Assert.Equal(100, invoice.MonthFee);
    }

    // ================================================================
    // BuildInvoiceData — QR & Metadata
    // ================================================================

    [Fact]
    public void BuildInvoiceData_HasQrPayload()
    {
        var b = CreateBuildingWithApt();
        var apt = b.Apartments[0];
        _wallet.EnsureMonth(b, "2025-01");

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        Assert.NotNull(invoice.QrPayload);
        Assert.Contains("BLD-001", invoice.QrPayload);
        Assert.Contains("2025-01", invoice.QrPayload);
    }

    [Fact]
    public void BuildInvoiceData_HasPaymentInfo()
    {
        var b = CreateBuildingWithApt();
        var apt = b.Apartments[0];
        _wallet.EnsureMonth(b, "2025-01");

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        Assert.Equal("Instapay", invoice.Payment.Label);
        Assert.Equal("123", invoice.Payment.AccountNumber);
    }

    [Fact]
    public void BuildInvoiceData_MonthLabelIsPopulated()
    {
        var b = CreateBuildingWithApt();
        var apt = b.Apartments[0];
        _wallet.EnsureMonth(b, "2025-01");

        var invoice = _service.BuildInvoiceData(b, apt, "2025-01");

        Assert.NotNull(invoice.MonthLabelText);
        Assert.NotEmpty(invoice.MonthLabelText);
    }
}