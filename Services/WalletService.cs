using System.Text.RegularExpressions;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/core/wallet.js + js/core/deposits.js
public class WalletService
{
    // -------- Month access (ensureMonth) --------
    public MonthData GetOrCreateMonth(Building building, string monthKey)
    {
        building.Months ??= new Dictionary<string, MonthData>();
        if (!building.Months.TryGetValue(monthKey, out var m))
        {
            m = new MonthData();
            building.Months[monthKey] = m;
        }
        return m;
    }

    public static string CurrentMonthKey() => DateTime.UtcNow.ToString("yyyy-MM");

    // -------- Deposits --------
    public string NextDepositNumber(Building building, string monthKey)
    {
        var m = GetOrCreateMonth(building, monthKey);
        var maxNum = 0;
        foreach (var d in m.Deposits)
        {
            var match = Regex.Match(d.Number ?? "", @"^D(\d+)$");
            if (match.Success) maxNum = Math.Max(maxNum, int.Parse(match.Groups[1].Value));
        }
        return "D" + (maxNum + 1).ToString("D3");
    }

    public Deposit CreateDeposit(Building building, string aptId, double amount, string? note,
     string createdBy, string monthKey, ReceiptData? receipt = null)
    {
        var m = GetOrCreateMonth(building, monthKey);
        var deposit = new Deposit
        {
            Id = "dep-" + Guid.NewGuid().ToString("N"),
            Number = NextDepositNumber(building, monthKey),
            AptId = aptId,
            Amount = amount,
            Note = note ?? "",
            Status = "pending",
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = createdBy,
            Receipt = receipt    // ← جديد
        };
        m.Deposits.Add(deposit);
        return deposit;
    }

    public Deposit? ConfirmDeposit(Building building, string depositId, string confirmedBy, string monthKey)
    {
        var m = GetOrCreateMonth(building, monthKey);
        var d = m.Deposits.FirstOrDefault(x => x.Id == depositId);
        if (d == null) return null;
        d.Status = "confirmed";
        d.ConfirmedAt = DateTime.UtcNow.ToString("o");
        d.ConfirmedBy = confirmedBy;
        return d;
    }

    public Deposit? CancelDeposit(Building building, string depositId, string cancelledBy, string? reason, string monthKey)
    {
        var m = GetOrCreateMonth(building, monthKey);
        var d = m.Deposits.FirstOrDefault(x => x.Id == depositId);
        if (d == null) return null;
        d.Status = "cancelled";
        d.CancelledAt = DateTime.UtcNow.ToString("o");
        d.CancelledBy = cancelledBy;
        d.CancelledReason = reason ?? "";
        return d;
    }

    public double TotalConfirmedDeposits(Building building, string aptId, string monthKey)
    {
        if (!building.Months.TryGetValue(monthKey, out var m)) return 0;
        return m.Deposits.Where(d => d.AptId == aptId && d.Status == "confirmed").Sum(d => d.Amount);
    }

    public List<Deposit> GetApartmentDeposits(Building building, string aptId, string monthKey)
    {
        var m = GetOrCreateMonth(building, monthKey);
        return m.Deposits.Where(d => d.AptId == aptId)
            .OrderByDescending(d => d.CreatedAt).ToList();
    }

    public List<(Apartment Apt, Deposit Deposit)> GetAllPendingDeposits(Building building, string monthKey)
    {
        if (!building.Months.TryGetValue(monthKey, out var m)) return new();
        var list = new List<(Apartment, Deposit)>();
        foreach (var d in m.Deposits.Where(d => d.Status == "pending"))
        {
            var apt = building.Apartments.FirstOrDefault(a => a.Id == d.AptId);
            if (apt != null) list.Add((apt, d));
        }
        return list;
    }

    // -------- Distribution --------
    public Dictionary<string, double> RecalculateDistribution(Building building, string monthKey)
    {
        var m = GetOrCreateMonth(building, monthKey);
        var openApts = building.Apartments.Where(a => !a.Closed).ToList();
        var totalExpenses = m.Expenses.Sum(e => e.Amount);
        var distribution = new Dictionary<string, double>();

        if (openApts.Count > 0)
        {
            if (totalExpenses > 0)
            {
                var share = totalExpenses / openApts.Count;
                foreach (var apt in openApts) distribution[apt.Id] = Math.Round(share, 2);
            }
            else
            {
                foreach (var apt in openApts) distribution[apt.Id] = 0;
            }
        }
        else
        {
            foreach (var a in building.Apartments) distribution[a.Id] = 0;
        }

        m.Distribution = distribution;
        return distribution;
    }

    public Dictionary<string, double> RecalculateRevenueDistribution(Building building, string monthKey)
    {
        var m = GetOrCreateMonth(building, monthKey);
        var openApts = building.Apartments.Where(a => !a.Closed).ToList();
        var distribution = new Dictionary<string, double>();

        if (openApts.Count > 0)
        {
            var totalRevenues = m.Revenues.Sum(r => r.Amount);
            if (totalRevenues > 0)
            {
                var share = totalRevenues / openApts.Count;
                foreach (var apt in openApts) distribution[apt.Id] = Math.Round(share, 2);
            }
            else
            {
                foreach (var apt in openApts) distribution[apt.Id] = 0;
            }
        }
        else
        {
            foreach (var a in building.Apartments) distribution[a.Id] = 0;
        }

        m.RevenueDistribution = distribution;
        return distribution;
    }

    // -------- Balance --------
    // الرصيد = (المدفوع - الرسم الشهري) - نصيب المصروفات + نصيب الإيرادات + التسويات (للشهر الحالي بس، من غير تراكم)
    public double ComputeWalletBalance(Building building, string aptId, string monthKey)
    {
        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return 0;
        if (!building.Months.TryGetValue(monthKey, out var m)) return 0;

        double balance = 0;
        var monthlyFee = apt.MonthlyFee;

        var totalPaid = m.Deposits.Where(d => d.AptId == aptId && d.Status == "confirmed").Sum(d => d.Amount);

        if (!apt.Closed && monthlyFee > 0) balance += totalPaid - monthlyFee;
        else balance += totalPaid;

        if (m.Distribution.TryGetValue(aptId, out var dist)) balance -= dist;
        if (m.RevenueDistribution.TryGetValue(aptId, out var revDist)) balance += revDist;

        var adjustments = building.WalletAdjustments.Where(a => a.AptId == aptId && a.MonthKey == monthKey);
        foreach (var adj in adjustments) balance += adj.Amount;

        return Math.Round(balance, 2);
    }

    // -------- Wallet Adjustments --------
    public WalletAdjustment AddWalletAdjustment(Building building, string aptId, double amount, string? reason, string monthKey, string createdBy)
    {
        building.WalletAdjustments ??= new List<WalletAdjustment>();
        var adj = new WalletAdjustment
        {
            Id = "adj-" + Guid.NewGuid().ToString("N"),
            AptId = aptId,
            Amount = amount,
            Reason = reason ?? "",
            MonthKey = monthKey,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = createdBy
        };
        building.WalletAdjustments.Add(adj);
        return adj;
    }

    public List<WalletAdjustment> GetApartmentAdjustments(Building building, string aptId) =>
        building.WalletAdjustments.Where(a => a.AptId == aptId).OrderByDescending(a => a.CreatedAt).ToList();

    // -------- Unified transactions (لكشف حساب الشقة) --------
    public List<WalletTransactionVm> GetUnifiedTransactions(Building building, string aptId, string monthKey)
    {
        var txs = new List<WalletTransactionVm>();
        if (!building.Months.TryGetValue(monthKey, out var m)) return txs;

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        var monthlyFee = apt?.MonthlyFee ?? 0;
        double totalPaid = 0;

        foreach (var d in m.Deposits.Where(d => d.AptId == aptId))
        {
            if (d.Status == "confirmed") totalPaid += d.Amount;
            txs.Add(new WalletTransactionVm
            {
                Type = "deposit", Status = d.Status, Id = d.Id, Number = d.Number,
                Amount = d.Amount, Note = d.Note, CreatedAt = d.CreatedAt
            });
        }

        if (apt != null && !apt.Closed && monthlyFee > 0)
        {
            var diff = totalPaid - monthlyFee;
            if (diff < 0)
                txs.Add(new WalletTransactionVm { Type = "monthly_fee_due", Id = "fee-due-" + monthKey, Amount = diff, Note = "رسم شهري مستحق", CreatedAt = monthKey + "-01T00:00:00Z" });
            else if (diff > 0)
                txs.Add(new WalletTransactionVm { Type = "monthly_fee_credit", Id = "fee-credit-" + monthKey, Amount = diff, Note = "زيادة في الرسم الشهري", CreatedAt = monthKey + "-01T00:00:00Z" });
        }

        if (m.Distribution.TryGetValue(aptId, out var dist) && dist > 0)
            txs.Add(new WalletTransactionVm { Type = "expense", Id = "expense-" + monthKey, Amount = -dist, Note = "نصيبك من مصروفات الشهر", CreatedAt = monthKey + "-28T23:59:59Z" });

        if (m.RevenueDistribution.TryGetValue(aptId, out var revDist) && revDist > 0)
            txs.Add(new WalletTransactionVm { Type = "revenue", Id = "revenue-" + monthKey, Amount = revDist, Note = "نصيبك من إيرادات العمارة", CreatedAt = monthKey + "-15T12:00:00Z" });

        foreach (var adj in building.WalletAdjustments.Where(a => a.AptId == aptId && a.MonthKey == monthKey))
            txs.Add(new WalletTransactionVm { Type = "adjustment", Id = adj.Id, Amount = adj.Amount, Note = adj.Reason, CreatedAt = adj.CreatedAt });

        return txs.OrderByDescending(t => t.CreatedAt).ToList();
    }

    // -------- Building-level totals --------
    public (double Collected, double Expenses, double Revenues, int OpenCount, int ClosedCount, double Balance, double TotalWallet)
        TotalsOf(Building building, string monthKey)
    {
        var m = GetOrCreateMonth(building, monthKey);
        var openApts = building.Apartments.Where(a => !a.Closed).ToList();

        var collected = building.Apartments.Sum(a => TotalConfirmedDeposits(building, a.Id, monthKey));
        var expenses = m.Expenses.Sum(e => e.Amount);
        var revenues = m.Revenues.Sum(r => r.Amount);
        var totalWallet = building.Apartments.Sum(a => ComputeWalletBalance(building, a.Id, monthKey));

        return (collected, expenses, revenues, openApts.Count,
            building.Apartments.Count - openApts.Count, collected + revenues - expenses, totalWallet);
    }
}
