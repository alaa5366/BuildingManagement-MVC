using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class WalletServiceAllTransactionsTests
{
    private static readonly WalletService Svc = new();
    private const string Month1 = "2024-11";
    private const string Month2 = "2024-12";
    private const string Month3 = "2025-01";

    private static Building CreateBuildingWithApts(params string[] aptIds)
    {
        var b = new Building { Name = "Test" };
        foreach (var id in aptIds)
        {
            b.Apartments.Add(new Apartment { Id = id, MonthlyFee = 100, Number = b.Apartments.Count + 1 });
        }
        return b;
    }

    // ================================================================
    // GetAllTransactions - حالات فارغة
    // ================================================================

    [Fact]
    public void GetAllTransactions_ReturnsEmpty_WhenBuildingHasNoApartments()
    {
        var b = new Building { Name = "Empty" };
        var result = Svc.GetAllTransactions(b, "a1");
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllTransactions_ReturnsEmpty_WhenApartmentNotFound()
    {
        var b = CreateBuildingWithApts("a1");
        var result = Svc.GetAllTransactions(b, "ghost");
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllTransactions_ReturnsEmpty_WhenNoMonthsExist()
    {
        var b = CreateBuildingWithApts("a1");
        var result = Svc.GetAllTransactions(b, "a1");
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllTransactions_ReturnsEmpty_WhenAllMonthsAreInTheFuture()
    {
        var b = CreateBuildingWithApts("a1");
        var futureMonth = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM");
        Svc.EnsureMonth(b, futureMonth);

        var result = Svc.GetAllTransactions(b, "a1");
        Assert.Empty(result); // الدالة بتستبعد الشهور المستقبلية
    }

    // ================================================================
    // GetAllTransactions - carry_over
    // ================================================================

    [Fact]
    public void GetAllTransactions_IncludesCarryOver_FromLatestMonthOnly()
    {
        var b = CreateBuildingWithApts("a1");
        var m1 = Svc.EnsureMonth(b, Month1);
        m1.CarryOver["a1"] = 50;
        var m2 = Svc.EnsureMonth(b, Month2);
        m2.CarryOver["a1"] = 75;

        var result = Svc.GetAllTransactions(b, "a1");
        var carry = result.FirstOrDefault(t => t.Type == "carry_over");

        Assert.NotNull(carry);
        Assert.Equal(75, carry.Amount); // من أحدث شهر (Month2)
    }

    [Fact]
    public void GetAllTransactions_ExcludesCarryOver_WhenZero()
    {
        var b = CreateBuildingWithApts("a1");
        Svc.EnsureMonth(b, Month1); // CarryOver هيكون 0 افتراضي

        var result = Svc.GetAllTransactions(b, "a1");
        Assert.DoesNotContain(result, t => t.Type == "carry_over");
    }

    // ================================================================
    // GetAllTransactions - deposits
    // ================================================================

    [Fact]
    public void GetAllTransactions_IncludesDepositsFromAllMonths()
    {
        var b = CreateBuildingWithApts("a1");

        var m1 = Svc.EnsureMonth(b, Month1);
        m1.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed", CreatedAt = "2024-11-01T10:00:00Z" });

        var m2 = Svc.EnsureMonth(b, Month2);
        m2.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "confirmed", CreatedAt = "2024-12-01T10:00:00Z" });

        var result = Svc.GetAllTransactions(b, "a1");
        var deposits = result.Where(t => t.Type == "deposit").ToList();

        Assert.Equal(2, deposits.Count);
        Assert.Contains(deposits, d => d.Id == "d1");
        Assert.Contains(deposits, d => d.Id == "d2");
    }

    [Fact]
    public void GetAllTransactions_FiltersByType_DepositsOnly()
    {
        var b = CreateBuildingWithApts("a1");
        var m = Svc.EnsureMonth(b, Month1);
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" });
        m.Distribution["a1"] = 50;

        var result = Svc.GetAllTransactions(b, "a1", typeFilter: "deposits");

        Assert.All(result, t => Assert.Equal("deposit", t.Type));
    }

    [Fact]
    public void GetAllTransactions_FiltersByStatus_PendingOnly()
    {
        var b = CreateBuildingWithApts("a1");
        var m = Svc.EnsureMonth(b, Month1);
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" });
        m.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "pending" });

        // نوع الفلتر "deposits,pending"
        var result = Svc.GetAllTransactions(b, "a1", typeFilter: "deposits,pending");

        Assert.Single(result);
        Assert.Equal("d2", result[0].Id);
    }

    [Fact]
    public void GetAllTransactions_FiltersByStatus_SuccessOnly()
    {
        var b = CreateBuildingWithApts("a1");
        var m = Svc.EnsureMonth(b, Month1);
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" });
        m.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "cancelled" });

        var result = Svc.GetAllTransactions(b, "a1", typeFilter: "deposits,success");

        Assert.Single(result);
        Assert.Equal("d1", result[0].Id);
    }

    // ================================================================
    // GetAllTransactions - monthly fee / distribution / revenue
    // ================================================================

    [Fact]
    public void GetAllTransactions_IncludesMonthlyFee_FromLatestMonthOnly()
    {
        var b = CreateBuildingWithApts("a1");
        Svc.EnsureMonth(b, Month1);
        var m2 = Svc.EnsureMonth(b, Month2);

        var result = Svc.GetAllTransactions(b, "a1");
        var fees = result.Where(t => t.Type == "monthly_fee_due").ToList();

        Assert.Single(fees); // مرة واحدة بس (من أحدث شهر)
        Assert.Equal(-100, fees[0].Amount);
    }

    [Fact]
    public void GetAllTransactions_IncludesExpenseShare_FromLatestMonthOnly()
    {
        var b = CreateBuildingWithApts("a1");
        var m1 = Svc.EnsureMonth(b, Month1);
        m1.Distribution["a1"] = 50; // شهر 1

        var m2 = Svc.EnsureMonth(b, Month2);
        m2.Distribution["a1"] = 75; // شهر 2 (الأحدث)

        var result = Svc.GetAllTransactions(b, "a1");
        var expenses = result.Where(t => t.Type == "expense").ToList();

        Assert.Single(expenses);
        Assert.Equal(-75, expenses[0].Amount); // من الشهر الأحدث فقط
    }

    [Fact]
    public void GetAllTransactions_IncludesRevenueShare_FromLatestMonthOnly()
    {
        var b = CreateBuildingWithApts("a1");
        var m1 = Svc.EnsureMonth(b, Month1);
        m1.RevenueDistribution["a1"] = 100;

        var m2 = Svc.EnsureMonth(b, Month2);
        m2.RevenueDistribution["a1"] = 200;

        var result = Svc.GetAllTransactions(b, "a1");
        var revenues = result.Where(t => t.Type == "revenue").ToList();

        Assert.Single(revenues);
        Assert.Equal(200, revenues[0].Amount);
    }

    // ================================================================
    // GetAllTransactions - date filters
    // ================================================================

    [Fact]
    public void GetAllTransactions_FromMonthFilter_ExcludesOlderMonths()
    {
        var b = CreateBuildingWithApts("a1");

        var m1 = Svc.EnsureMonth(b, Month1);
        m1.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" });

        var m2 = Svc.EnsureMonth(b, Month2);
        m2.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "confirmed" });

        var result = Svc.GetAllTransactions(b, "a1", fromMonth: Month2);
        var deposits = result.Where(t => t.Type == "deposit").ToList();

        Assert.Single(deposits);
        Assert.Equal("d2", deposits[0].Id);
    }

    [Fact]
    public void GetAllTransactions_ToMonthFilter_ExcludesNewerMonths()
    {
        var b = CreateBuildingWithApts("a1");

        var m1 = Svc.EnsureMonth(b, Month1);
        m1.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" });

        var m2 = Svc.EnsureMonth(b, Month2);
        m2.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "confirmed" });

        var result = Svc.GetAllTransactions(b, "a1", toMonth: Month1);
        var deposits = result.Where(t => t.Type == "deposit").ToList();

        Assert.Single(deposits);
        Assert.Equal("d1", deposits[0].Id);
    }

    // ================================================================
    // GetAllTransactions - ordering
    // ================================================================

    [Fact]
    public void GetAllTransactions_OrdersByDateDescending()
    {
        var b = CreateBuildingWithApts("a1");
        var m = Svc.EnsureMonth(b, Month1);
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed", CreatedAt = "2024-11-01T10:00:00Z" });
        m.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "confirmed", CreatedAt = "2024-11-05T10:00:00Z" });

        var result = Svc.GetAllTransactions(b, "a1");
        var deposits = result.Where(t => t.Type == "deposit").ToList();

        Assert.Equal("d2", deposits[0].Id); // الأحدث الأول
        Assert.Equal("d1", deposits[1].Id);
    }
}