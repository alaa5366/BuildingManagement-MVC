using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class WalletServiceTests
{
    private static readonly WalletService Svc = new();
    private const string Month = "2025-01";
    private const string PrevMonth = "2024-12";

    // ================================================================
    // Helpers
    // ================================================================
    private static Building BuildingWithApartments(params (string id, bool closed, double fee)[] apts)
    {
        var b = new Building { Name = "Test Tower" };
        foreach (var (id, closed, fee) in apts)
            b.Apartments.Add(new Apartment
            {
                Id = id,
                Closed = closed,
                MonthlyFee = fee,
                Number = b.Apartments.Count + 1
            });
        return b;
    }

    private static Building EmptyBuilding() => new() { Name = "Empty" };

    // ================================================================
    // EnsureMonth
    // ================================================================

    [Fact]
    public void EnsureMonth_NewMonth_CreatesZeroCarryOverForAllApartments()
    {
        var b = BuildingWithApartments(("a1", false, 100), ("a2", false, 200));

        var m = Svc.EnsureMonth(b, Month);

        Assert.NotNull(m);
        Assert.True(b.Months.ContainsKey(Month));
        Assert.Equal(0, m.CarryOver["a1"]);
        Assert.Equal(0, m.CarryOver["a2"]);
    }

    [Fact]
    public void EnsureMonth_ExistingMonth_ReturnsSameInstance()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var first = Svc.EnsureMonth(b, Month);
        first.CarryOver["a1"] = 50;

        var second = Svc.EnsureMonth(b, Month);

        Assert.Same(first, second);
        Assert.Equal(50, second.CarryOver["a1"]);
    }

    [Fact]
	public void EnsureMonth_ExistingMonthWithEmptyCarryOver_RecomputesFromPrevious()
{
    // fee=100، دفعة=200 ⇒ رصيد متبقي = 100 (يترحّل)
    var b = BuildingWithApartments(("a1", false, 100));

    // شهر سابق
    var prev = Svc.EnsureMonth(b, PrevMonth);
    prev.Deposits.Add(new Deposit
    {
        Id = "d1", AptId = "a1", Amount = 200, Status = "confirmed"
    });

    // نضيف الشهر الجديد يدوياً مع CarryOver فاضي
    b.Months[Month] = new MonthData { CarryOver = new() };

    var m = Svc.EnsureMonth(b, Month);

    // لازم يتحسب من الشهر السابق: 200 - 100 = 100
    Assert.True(m.CarryOver.ContainsKey("a1"));
    Assert.Equal(100, m.CarryOver["a1"]);
}

    [Fact]
    public void EnsureMonth_NoPreviousMonth_InitializesZeroForAll()
    {
        var b = BuildingWithApartments(("a1", false, 0), ("a2", false, 0));

        var m = Svc.EnsureMonth(b, Month);

        Assert.Equal(2, m.CarryOver.Count);
        Assert.All(m.CarryOver.Values, v => Assert.Equal(0, v));
    }

    [Fact]
    public void EnsureMonth_EmptyBuilding_DoesNotThrow()
    {
        var b = EmptyBuilding();

        var m = Svc.EnsureMonth(b, Month);

        Assert.NotNull(m);
        Assert.NotNull(m.CarryOver);
        Assert.Empty(m.CarryOver);
    }
	
	[Fact]
public void EnsureMonth_ExistingMonthWithEmptyCarryOver_DepositEqualsFee_YieldsZeroCarryOver()
{
    // ⚠️ سلوك مهم: دفعة = الرسم ⇒ صافي الرصيد = صفر ⇒ CarryOver = صفر
    // (ده مش bug — ده المنطق المالي الصحيح)
    var b = BuildingWithApartments(("a1", false, 100));

    var prev = Svc.EnsureMonth(b, PrevMonth);
    prev.Deposits.Add(new Deposit
    {
        Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed"
    });

    b.Months[Month] = new MonthData { CarryOver = new() };

    var m = Svc.EnsureMonth(b, Month);

    Assert.True(m.CarryOver.ContainsKey("a1"));
    Assert.Equal(0, m.CarryOver["a1"]);  // ← مفيش رصيد متبقي
}

    // ================================================================
    // NextDepositNumber
    // ================================================================

    [Fact]
    public void NextDepositNumber_NoDeposits_ReturnsD001()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        Assert.Equal("D001", Svc.NextDepositNumber(b, Month));
    }

    [Fact]
    public void NextDepositNumber_AfterD001_ReturnsD002()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var m = Svc.EnsureMonth(b, Month);
        m.Deposits.Add(new Deposit { Id = "x", AptId = "a1", Number = "D001", Amount = 1 });

        Assert.Equal("D002", Svc.NextDepositNumber(b, Month));
    }

    [Fact]
    public void NextDepositNumber_UsesMaxNotLast()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var m = Svc.EnsureMonth(b, Month);
        m.Deposits.Add(new Deposit { Id = "x1", AptId = "a1", Number = "D005", Amount = 1 });
        m.Deposits.Add(new Deposit { Id = "x2", AptId = "a1", Number = "D012", Amount = 1 });
        m.Deposits.Add(new Deposit { Id = "x3", AptId = "a1", Number = "D003", Amount = 1 });

        Assert.Equal("D013", Svc.NextDepositNumber(b, Month));
    }

    [Theory]
    [InlineData("DABC")]
    [InlineData("D12X")]
    [InlineData("")]
    [InlineData("X001")]
    [InlineData("001")]
    public void NextDepositNumber_IgnoresInvalidFormats(string invalidNumber)
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var m = Svc.EnsureMonth(b, Month);
        m.Deposits.Add(new Deposit { Id = "x", AptId = "a1", Number = invalidNumber, Amount = 1 });

        Assert.Equal("D001", Svc.NextDepositNumber(b, Month));
    }

    [Fact]
    public void NextDepositNumber_NullNumber_IsIgnored()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var m = Svc.EnsureMonth(b, Month);
        m.Deposits.Add(new Deposit { Id = "x", AptId = "a1", Number = null!, Amount = 1 });

        Assert.Equal("D001", Svc.NextDepositNumber(b, Month));
    }

    // ================================================================
    // CreateDeposit
    // ================================================================

    [Fact]
    public void CreateDeposit_ValidData_ReturnsPendingDeposit()
    {
        var b = BuildingWithApartments(("a1", false, 100));

        var d = Svc.CreateDeposit(b, "a1", 500, "January", "admin@test.com", Month);

        Assert.NotNull(d);
        Assert.Equal("pending", d.Status);
        Assert.Equal("a1", d.AptId);
        Assert.Equal(500, d.Amount);
        Assert.Equal("January", d.Note);
        Assert.Equal("admin@test.com", d.CreatedBy);
        Assert.Equal("D001", d.Number);
        Assert.StartsWith("dep-", d.Id);
    }

    [Fact]
    public void CreateDeposit_AddsToMonthDeposits()
    {
        var b = BuildingWithApartments(("a1", false, 100));

        Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);

        var m = b.Months[Month];
        Assert.Single(m.Deposits);
    }

    [Fact]
    public void CreateDeposit_MultipleDeposits_GetIncrementingNumbers()
    {
        var b = BuildingWithApartments(("a1", false, 100));

        var d1 = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);
        var d2 = Svc.CreateDeposit(b, "a1", 200, null, "admin", Month);
        var d3 = Svc.CreateDeposit(b, "a1", 300, null, "admin", Month);

        Assert.Equal("D001", d1.Number);
        Assert.Equal("D002", d2.Number);
        Assert.Equal("D003", d3.Number);
    }

    [Fact]
    public void CreateDeposit_GeneratesUniqueIds()
    {
        var b = BuildingWithApartments(("a1", false, 100));

        var d1 = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);
        var d2 = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);

        Assert.NotEqual(d1.Id, d2.Id);
    }

    [Fact]
    public void CreateDeposit_WithReceipt_StoresReceipt()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var receipt = new ReceiptData { Success = true, Url = "https://cdn.test/r.jpg" };

        var d = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month, receipt);

        Assert.NotNull(d.Receipt);
        Assert.True(d.Receipt!.Success);
        Assert.Equal("https://cdn.test/r.jpg", d.Receipt.Url);
    }

    [Fact]
    public void CreateDeposit_NullNote_StoredAsEmptyString()
    {
        var b = BuildingWithApartments(("a1", false, 100));

        var d = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);

        Assert.Equal("", d.Note);
    }

    // ================================================================
    // ConfirmDeposit
    // ================================================================

    [Fact]
    public void ConfirmDeposit_PendingDeposit_SetsConfirmedStatus()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var d = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);

        var result = Svc.ConfirmDeposit(b, d.Id, "manager@test.com", Month);

        Assert.NotNull(result);
        Assert.Equal("confirmed", result!.Status);
        Assert.Equal("manager@test.com", result.ConfirmedBy);
        Assert.NotNull(result.ConfirmedAt);
    }

    [Fact]
    public void ConfirmDeposit_UnknownId_ReturnsNull()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        Svc.EnsureMonth(b, Month);

        var result = Svc.ConfirmDeposit(b, "does-not-exist", "manager", Month);

        Assert.Null(result);
    }

    [Fact]
    public void ConfirmDeposit_AlreadyConfirmed_RemainsConfirmed()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var d = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);
        Svc.ConfirmDeposit(b, d.Id, "m1", Month);

        var result = Svc.ConfirmDeposit(b, d.Id, "m2", Month);

        Assert.NotNull(result);
        Assert.Equal("confirmed", result!.Status);
        Assert.Equal("m2", result.ConfirmedBy);  // آخر تأكيد بيفوز
    }

    // ================================================================
    // CancelDeposit
    // ================================================================

    [Fact]
    public void CancelDeposit_PendingDeposit_SetsCancelledStatus()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var d = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);

        var result = Svc.CancelDeposit(b, d.Id, "manager", "duplicate", Month);

        Assert.NotNull(result);
        Assert.Equal("cancelled", result!.Status);
        Assert.Equal("manager", result.CancelledBy);
        Assert.Equal("duplicate", result.CancelledReason);
        Assert.NotNull(result.CancelledAt);
    }

    [Fact]
    public void CancelDeposit_NullReason_StoredAsEmptyString()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var d = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);

        var result = Svc.CancelDeposit(b, d.Id, "manager", null, Month);

        Assert.Equal("", result!.CancelledReason);
    }

    [Fact]
    public void CancelDeposit_UnknownId_ReturnsNull()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        Svc.EnsureMonth(b, Month);

        var result = Svc.CancelDeposit(b, "nope", "manager", null, Month);

        Assert.Null(result);
    }

    // ================================================================
    // UpdatePendingDeposit
    // ================================================================

    [Fact]
    public void UpdatePendingDeposit_ChangesAmountAndNote()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var d = Svc.CreateDeposit(b, "a1", 100, "old", "admin", Month);

        var result = Svc.UpdatePendingDeposit(b, d.Id, Month, 250, "new", "typo", "admin");

        Assert.NotNull(result);
        Assert.Equal(250, result!.Amount);
        Assert.Equal("new", result.Note);
        Assert.Equal("typo", result.UpdateReason);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public void UpdatePendingDeposit_AddsEditHistoryEntry()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var d = Svc.CreateDeposit(b, "a1", 100, "old", "admin", Month);

        Svc.UpdatePendingDeposit(b, d.Id, Month, 250, "new", "typo", "admin");

        Assert.NotNull(d.EditHistory);
        Assert.Single(d.EditHistory!);
        var entry = d.EditHistory![0];
        Assert.Equal(100, entry.OldAmount);
        Assert.Equal(250, entry.NewAmount);
        Assert.Equal("old", entry.OldNote);
        Assert.Equal("new", entry.NewNote);
        Assert.Equal("typo", entry.Reason);
        Assert.Equal("admin", entry.By);
    }

    [Fact]
    public void UpdatePendingDeposit_ConfirmedDeposit_Throws()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var d = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);
        Svc.ConfirmDeposit(b, d.Id, "manager", Month);

        Assert.Throws<InvalidOperationException>(() =>
            Svc.UpdatePendingDeposit(b, d.Id, Month, 200, "x", "reason", "admin"));
    }

    [Fact]
    public void UpdatePendingDeposit_CancelledDeposit_Throws()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var d = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);
        Svc.CancelDeposit(b, d.Id, "manager", null, Month);

        Assert.Throws<InvalidOperationException>(() =>
            Svc.UpdatePendingDeposit(b, d.Id, Month, 200, "x", "reason", "admin"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void UpdatePendingDeposit_NonPositiveAmount_Throws(double amount)
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var d = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);

        Assert.Throws<InvalidOperationException>(() =>
            Svc.UpdatePendingDeposit(b, d.Id, Month, amount, "x", "reason", "admin"));
    }

    [Fact]
    public void UpdatePendingDeposit_UnknownId_ReturnsNull()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        Svc.EnsureMonth(b, Month);

        var result = Svc.UpdatePendingDeposit(b, "ghost", Month, 100, "x", "r", "admin");

        Assert.Null(result);
    }

    // ================================================================
    // ComputeWalletBalance — الأساسيات
    // ================================================================

    [Fact]
    public void ComputeWalletBalance_UnknownApartment_ReturnsZero()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        Svc.EnsureMonth(b, Month);

        Assert.Equal(0, Svc.ComputeWalletBalance(b, "ghost", Month));
    }

    [Fact]
    public void ComputeWalletBalance_UnknownMonth_ReturnsZero()
    {
        var b = BuildingWithApartments(("a1", false, 100));

        Assert.Equal(0, Svc.ComputeWalletBalance(b, "a1", "2099-01"));
    }

    [Fact]
    public void ComputeWalletBalance_ClosedApartment_NoMonthlyFeeDeducted()
    {
        var b = BuildingWithApartments(("a1", true, 100));  // closed
        var m = Svc.EnsureMonth(b, Month);
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" });

        var balance = Svc.ComputeWalletBalance(b, "a1", Month);

        // مفيش خصم للرسم (closed) ⇒ 500 صافي
        Assert.Equal(500, balance);
    }

    [Fact]
    public void ComputeWalletBalance_OpenApartment_DeductsMonthlyFee()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var m = Svc.EnsureMonth(b, Month);
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" });

        var balance = Svc.ComputeWalletBalance(b, "a1", Month);

        // 500 (دفعة) - 100 (رسم) = 400
        Assert.Equal(400, balance);
    }

    [Fact]
    public void ComputeWalletBalance_OnlyPendingDeposits_NotCounted()
    {
        var b = BuildingWithApartments(("a1", false, 0));  // fee=0 لتبسيط الحساب
        var m = Svc.EnsureMonth(b, Month);
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "pending" });

        Assert.Equal(0, Svc.ComputeWalletBalance(b, "a1", Month));
    }

    [Fact]
    public void ComputeWalletBalance_ExpenseDistribution_Deducted()
    {
        var b = BuildingWithApartments(("a1", false, 0));
        var m = Svc.EnsureMonth(b, Month);
        m.Distribution["a1"] = 150;

        Assert.Equal(-150, Svc.ComputeWalletBalance(b, "a1", Month));
    }

    [Fact]
    public void ComputeWalletBalance_RevenueDistribution_Added()
    {
        var b = BuildingWithApartments(("a1", false, 0));
        var m = Svc.EnsureMonth(b, Month);
        m.RevenueDistribution["a1"] = 200;

        Assert.Equal(200, Svc.ComputeWalletBalance(b, "a1", Month));
    }

    [Fact]
    public void ComputeWalletBalance_WalletAdjustments_Summed()
    {
        var b = BuildingWithApartments(("a1", false, 0));
        Svc.EnsureMonth(b, Month);
        Svc.AddWalletAdjustment(b, "a1", 50, "credit", Month, "admin");
        Svc.AddWalletAdjustment(b, "a1", -30, "debit", Month, "admin");

        Assert.Equal(20, Svc.ComputeWalletBalance(b, "a1", Month));
    }

    [Fact]
    public void ComputeWalletBalance_CarryOver_Added()
    {
        var b = BuildingWithApartments(("a1", false, 0));
        var m = Svc.EnsureMonth(b, Month);
        m.CarryOver["a1"] = 75;

        Assert.Equal(75, Svc.ComputeWalletBalance(b, "a1", Month));
    }

    [Fact]
    public void ComputeWalletBalance_FullCombination_IsCorrect()
    {
        var b = BuildingWithApartments(("a1", false, 100));
        var m = Svc.EnsureMonth(b, Month);

        // + carry 75
        m.CarryOver["a1"] = 75;
        // + 500 deposit
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 500, Status = "confirmed" });
        // - 100 monthly fee (open)
        // - 150 expense share
        m.Distribution["a1"] = 150;
        // + 200 revenue share
        m.RevenueDistribution["a1"] = 200;
        // + 50 adjustment
        Svc.AddWalletAdjustment(b, "a1", 50, "bonus", Month, "admin");

        // المتوقع: 75 + 500 - 100 - 150 + 200 + 50 = 575
        Assert.Equal(575, Svc.ComputeWalletBalance(b, "a1", Month));
    }

    // ================================================================
    // AddWalletAdjustment
    // ================================================================

    [Fact]
    public void AddWalletAdjustment_StoresAllFields()
    {
        var b = BuildingWithApartments(("a1", false, 0));

        var adj = Svc.AddWalletAdjustment(b, "a1", -25.5, "late fee", Month, "admin");

        Assert.NotNull(adj);
        Assert.StartsWith("adj-", adj.Id);
        Assert.Equal("a1", adj.AptId);
        Assert.Equal(-25.5, adj.Amount);
        Assert.Equal("late fee", adj.Reason);
        Assert.Equal(Month, adj.MonthKey);
        Assert.Equal("admin", adj.CreatedBy);
        Assert.Single(b.WalletAdjustments);
    }

    [Fact]
    public void AddWalletAdjustment_NullReason_StoredAsEmptyString()
    {
        var b = BuildingWithApartments(("a1", false, 0));

        var adj = Svc.AddWalletAdjustment(b, "a1", 10, null, Month, "admin");

        Assert.Equal("", adj.Reason);
    }

    // ================================================================
    // RecalculateDistribution
    // ================================================================

    [Fact]
    public void RecalculateDistribution_EvenlySplitsAmongOpenApartments()
    {
        var b = BuildingWithApartments(("a1", false, 0), ("a2", false, 0), ("a3", false, 0));
        var m = Svc.EnsureMonth(b, Month);
        m.Expenses.Add(new Expense { Id = "e1", Amount = 300 });

        var dist = Svc.RecalculateDistribution(b, Month);

        Assert.Equal(3, dist.Count);
        Assert.All(dist.Values, v => Assert.Equal(100, v));
    }

    [Fact]
    public void RecalculateDistribution_ExcludesClosedApartments()
    {
        var b = BuildingWithApartments(("a1", false, 0), ("a2", true, 0));  // a2 closed
        var m = Svc.EnsureMonth(b, Month);
        m.Expenses.Add(new Expense { Id = "e1", Amount = 100 });

        var dist = Svc.RecalculateDistribution(b, Month);

        Assert.Equal(100, dist["a1"]);
        Assert.False(dist.ContainsKey("a2"));  // مش موجود في التوزيع
    }

    [Fact]
    public void RecalculateDistribution_AllClosed_ZeroForEveryApartment()
    {
        var b = BuildingWithApartments(("a1", true, 0), ("a2", true, 0));
        var m = Svc.EnsureMonth(b, Month);
        m.Expenses.Add(new Expense { Id = "e1", Amount = 100 });

        var dist = Svc.RecalculateDistribution(b, Month);

        // كل الشقق مقفولة ⇒ الكود بيرجع صفر للكل (fallback)
        Assert.Equal(2, dist.Count);
        Assert.All(dist.Values, v => Assert.Equal(0, v));
    }

    [Fact]
    public void RecalculateDistribution_NoExpenses_ZeroForAll()
    {
        var b = BuildingWithApartments(("a1", false, 0), ("a2", false, 0));
        Svc.EnsureMonth(b, Month);

        var dist = Svc.RecalculateDistribution(b, Month);

        Assert.All(dist.Values, v => Assert.Equal(0, v));
    }

    // ================================================================
    // RecalculateRevenueDistribution
    // ================================================================

    [Fact]
    public void RecalculateRevenueDistribution_EvenlySplitsRevenues()
    {
        var b = BuildingWithApartments(("a1", false, 0), ("a2", false, 0));
        var m = Svc.EnsureMonth(b, Month);
        m.Revenues.Add(new Revenue { Id = "r1", Amount = 200 });

        var dist = Svc.RecalculateRevenueDistribution(b, Month);

        Assert.Equal(2, dist.Count);
        Assert.All(dist.Values, v => Assert.Equal(100, v));
    }

    // ================================================================
    // ComputeCarryOverForNextMonth
    // ================================================================

    [Fact]
    public void ComputeCarryOverForNextMonth_NoPreviousMonth_ZeroForAll()
    {
        var b = BuildingWithApartments(("a1", false, 0), ("a2", false, 0));

        var carry = Svc.ComputeCarryOverForNextMonth(b, "2099-12");

        Assert.Equal(2, carry.Count);
        Assert.All(carry.Values, v => Assert.Equal(0, v));
    }

    [Fact]
    public void ComputeCarryOverForNextMonth_CarriesEachApartmentBalance()
    {
        var b = BuildingWithApartments(("a1", false, 0), ("a2", false, 0));
        var m = Svc.EnsureMonth(b, PrevMonth);
        // a1: +100 (deposit) - a2: صفر
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "confirmed" });

        var carry = Svc.ComputeCarryOverForNextMonth(b, PrevMonth);

        Assert.Equal(100, carry["a1"]);
        Assert.Equal(0, carry["a2"]);
    }

    // ================================================================
    // TotalsOf
    // ================================================================

    [Fact]
    public void TotalsOf_SumsCollectedExpensesAndRevenues()
    {
        var b = BuildingWithApartments(("a1", false, 0), ("a2", false, 0));
        var m = Svc.EnsureMonth(b, Month);
        m.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 300, Status = "confirmed" });
        m.Deposits.Add(new Deposit { Id = "d2", AptId = "a2", Amount = 200, Status = "confirmed" });
        m.Expenses.Add(new Expense { Id = "e1", Amount = 100 });
        m.Revenues.Add(new Revenue { Id = "r1", Amount = 50 });

        var (collected, expenses, revenues, openCount, closedCount, _, _) =
            Svc.TotalsOf(b, Month);

        Assert.Equal(500, collected);
        Assert.Equal(100, expenses);
        Assert.Equal(50, revenues);
        Assert.Equal(2, openCount);
        Assert.Equal(0, closedCount);
    }

    [Fact]
    public void TotalsOf_CountsClosedSeparately()
    {
        var b = BuildingWithApartments(("a1", false, 0), ("a2", true, 0), ("a3", true, 0));
        Svc.EnsureMonth(b, Month);

        var (_, _, _, openCount, closedCount, _, _) = Svc.TotalsOf(b, Month);

        Assert.Equal(1, openCount);
        Assert.Equal(2, closedCount);
    }

    // ================================================================
    // GetAllPendingDeposits
    // ================================================================

    [Fact]
    public void GetAllPendingDeposits_FiltersOnlyPending()
    {
        var b = BuildingWithApartments(("a1", false, 0));
        var d1 = Svc.CreateDeposit(b, "a1", 100, null, "admin", Month);
        Svc.CreateDeposit(b, "a1", 200, null, "admin", Month);  // pending
        Svc.ConfirmDeposit(b, d1.Id, "m", Month);

        var pending = Svc.GetAllPendingDeposits(b, Month);

        Assert.Single(pending);
        Assert.Equal(200, pending[0].Deposit.Amount);
    }

    [Fact]
    public void GetAllPendingDeposits_EmptyMonth_ReturnsEmptyList()
    {
        var b = BuildingWithApartments(("a1", false, 0));

        var pending = Svc.GetAllPendingDeposits(b, "2099-01");

        Assert.Empty(pending);
    }

    // ================================================================
    // GetAllPendingDepositsCrossMonth
    // ================================================================

    [Fact]
    public void GetAllPendingDepositsCrossMonth_IncludesMultipleMonths()
    {
        var b = BuildingWithApartments(("a1", false, 0));
        var m1 = Svc.EnsureMonth(b, "2024-11");
        m1.Deposits.Add(new Deposit { Id = "d1", AptId = "a1", Amount = 100, Status = "pending" });
        var m2 = Svc.EnsureMonth(b, "2024-12");
        m2.Deposits.Add(new Deposit { Id = "d2", AptId = "a1", Amount = 200, Status = "pending" });

        var list = Svc.GetAllPendingDepositsCrossMonth(b);

        Assert.Equal(2, list.Count);
    }

    // ================================================================
    // CurrentMonthKey
    // ================================================================

    [Fact]
    public void CurrentMonthKey_ReturnsUtcYearMonthFormat()
    {
        var key = WalletService.CurrentMonthKey();
        var now = DateTime.UtcNow;

        Assert.Equal(now.ToString("yyyy-MM"), key);
    }
}