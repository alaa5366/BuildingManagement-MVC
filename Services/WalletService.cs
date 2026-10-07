using System.Text.RegularExpressions;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/core/wallet.js + js/core/deposits.js
public class WalletService
{
    // -------- Month access (ensureMonth) --------
    public MonthData GetOrCreateMonth(Building building, string monthKey)
    {
        return EnsureMonth(building, monthKey);
    }

    public MonthData EnsureMonth(Building building, string monthKey)
    {
        building.Months ??= new Dictionary<string, MonthData>();

        if (building.Months.TryGetValue(monthKey, out var existing))
        {
            // ✅ لو carryOver فاضي، احسبه
            if (existing.CarryOver == null || existing.CarryOver.Count == 0)
            {
                var prevKey = GetPreviousMonthKey(building, monthKey);
                if (prevKey != null)
                {
                    existing.CarryOver = ComputeCarryOverForNextMonth(building, prevKey);
                }
                else
                {
                    existing.CarryOver = new Dictionary<string, double>();
                    foreach (var apt in building.Apartments)
                        existing.CarryOver[apt.Id] = 0;
                }
            }

            return existing;
        }

        // ✅ شهر جديد
        var newMonth = new MonthData();
        var previousKey = GetPreviousMonthKey(building, monthKey);

        if (previousKey != null)
        {
            newMonth.CarryOver = ComputeCarryOverForNextMonth(building, previousKey);
        }
        else
        {
            foreach (var apt in building.Apartments)
                newMonth.CarryOver[apt.Id] = 0;
        }

        building.Months[monthKey] = newMonth;
        return newMonth;
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
            Receipt = receipt
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

    public Deposit? UpdatePendingDeposit(
        Building building,
        string depositId,
        string monthKey,
        double newAmount,
        string newNote,
        string reason,
        string updatedBy)
    {
        var m = GetOrCreateMonth(building, monthKey);
        var d = m.Deposits.FirstOrDefault(x => x.Id == depositId);
        if (d == null) return null;

        if (d.Status != "pending")
            throw new InvalidOperationException(Loc.T("A_Confirmed_Or_Cancelled_Payment_Cannot"));

        if (newAmount <= 0)
            throw new InvalidOperationException(Loc.T("The_Amount_Must_Be_Greater_Than"));

        var editEntry = new DepositEditEntry
        {
            Ts = DateTime.UtcNow.ToString("o"),
            By = updatedBy,
            Reason = reason,
            OldAmount = d.Amount,
            NewAmount = newAmount,
            OldNote = d.Note,
            NewNote = newNote
        };

        d.EditHistory ??= new List<DepositEditEntry>();
        d.EditHistory.Add(editEntry);

        d.Amount = newAmount;
        d.Note = newNote;
        d.UpdatedAt = DateTime.UtcNow.ToString("o");
        d.UpdatedBy = updatedBy;
        d.UpdateReason = reason;

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
    public double ComputeWalletBalance(Building building, string aptId, string monthKey)
    {
        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return 0;
        if (!building.Months.TryGetValue(monthKey, out var m)) return 0;

        double balance = 0;

        if (m.CarryOver != null && m.CarryOver.TryGetValue(aptId, out var carry))
            balance += carry;

        var monthlyFee = apt.MonthlyFee;
        var totalPaid = m.Deposits
            .Where(d => d.AptId == aptId && d.Status == "confirmed")
            .Sum(d => d.Amount);

        if (!apt.Closed && monthlyFee > 0)
            balance += totalPaid - monthlyFee;
        else
            balance += totalPaid;

        if (m.Distribution.TryGetValue(aptId, out var dist))
            balance -= dist;

        if (m.RevenueDistribution.TryGetValue(aptId, out var revDist))
            balance += revDist;

        var adjustments = building.WalletAdjustments
            .Where(a => a.AptId == aptId && a.MonthKey == monthKey);
        foreach (var adj in adjustments)
            balance += adj.Amount;

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

    // ============================================================
    // ✅ Unified transactions (لكشف حساب الشقة — شهر واحد)
    // ============================================================
    public List<WalletTransactionVm> GetUnifiedTransactions(Building building, string aptId, string monthKey)
    {
        var txs = new List<WalletTransactionVm>();
        if (!building.Months.TryGetValue(monthKey, out var m)) return txs;

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        var monthlyFee = apt?.MonthlyFee ?? 0;
        var now = DateTime.UtcNow.ToString("o");

        // ✅ 1. الرصيد السابق
        if (m.CarryOver != null && m.CarryOver.TryGetValue(aptId, out var carry) && carry != 0)
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "carry_over",
                Status = "confirmed",
                Id = "carry-" + monthKey,
                Amount = carry,
                Note = Loc.T("Previous_Balance"),
                CreatedAt = now
            });
        }

        // ✅ 2. الرسم الشهري
        if (apt != null && !apt.Closed && monthlyFee > 0)
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "monthly_fee_due",
                Status = "confirmed",
                Id = "fee-" + monthKey,
                Amount = -monthlyFee,
                Note = Loc.T("Monthly_Fee_Due"),
                CreatedAt = now
            });
        }

        // ✅ 3. نصيب الإيرادات
        if (m.RevenueDistribution.TryGetValue(aptId, out var revDist) && revDist > 0)
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "revenue",
                Status = "confirmed",
                Id = "revenue-" + monthKey,
                Amount = revDist,
                Note = Loc.T("Your_Share_Of_The_Building_S"),
                CreatedAt = now
            });
        }

        // ✅ 4. نصيب المصاريف
        if (m.Distribution.TryGetValue(aptId, out var dist) && dist > 0)
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "expense",
                Status = "confirmed",
                Id = "expense-" + monthKey,
                Amount = -dist,
                Note = Loc.T("Your_Share_Of_This_Month_S"),
                CreatedAt = now
            });
        }

        // ✅ 5. الدفعات (تاريخها الفعلي)
        foreach (var d in m.Deposits.Where(d => d.AptId == aptId))
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "deposit",
                Status = d.Status,
                Id = d.Id,
                Number = d.Number,
                Amount = d.Amount,
                Note = d.Note,
                CreatedAt = d.CreatedAt,
                ReceiptUrl = d.Receipt?.Success == true ? d.Receipt.Url : null
            });
        }

        // ✅ 6. التسويات
        foreach (var adj in building.WalletAdjustments.Where(a => a.AptId == aptId && a.MonthKey == monthKey))
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "adjustment",
                Status = "confirmed",
                Id = adj.Id,
                Amount = adj.Amount,
                Note = adj.Reason,
                CreatedAt = adj.CreatedAt
            });
        }

        return OrderTransactions(txs);
    }

    // ============================================================
    // ✅ جميع الحركات لكل الشهور
    //
    // ⚠️ ملاحظة: الرسم الشهري، الإيرادات، المصروفات، الرصيد السابق
    //    تُحسب **للشهر الأحدث فقط** (لأن السابق يُلخّص في "الرصيد السابق").
    //    الدفعات والتسويات تُعرض لكل الشهور (تواريخها الفعلية).
    // ============================================================
    public List<WalletTransactionVm> GetAllTransactions(
        Building building,
        string aptId,
        string? fromMonth = null,
        string? toMonth = null,
        string? typeFilter = null,
        string? statusFilter = null)
    {
        var txs = new List<WalletTransactionVm>();
        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId);
        if (apt == null) return txs;

        var selectedTypes = (typeFilter ?? "all")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .ToList();
        var isAll = selectedTypes.Contains("all") || selectedTypes.Count == 0;

        var currentMonth = CurrentMonthKey();
        var now = DateTime.UtcNow.ToString("o");

        // ✅ قائمة الشهور (مرتبة تنازليًا — الأحدث أولًا)
        var allMonths = building.Months.Keys
            .Where(k => string.Compare(k, currentMonth, StringComparison.Ordinal) <= 0)
            .Where(k => string.IsNullOrWhiteSpace(fromMonth) ||
                        string.Compare(k, fromMonth, StringComparison.Ordinal) >= 0)
            .Where(k => string.IsNullOrWhiteSpace(toMonth) ||
                        string.Compare(k, toMonth, StringComparison.Ordinal) <= 0)
            .OrderByDescending(k => k)
            .ToList();

        // ✅ آخر شهر (الأحدث) — للحركات الدورية
        var latestMonthKey = allMonths.FirstOrDefault();
        if (string.IsNullOrEmpty(latestMonthKey)) return txs;

        var latestMonth = building.Months[latestMonthKey];
        var monthlyFee = apt.MonthlyFee;

        // ✅ 1. الرصيد السابق (carry_over) — مرة واحدة فقط
        if ((isAll || selectedTypes.Contains("carry_over")) &&
            latestMonth.CarryOver != null &&
            latestMonth.CarryOver.TryGetValue(aptId, out var carry) &&
            carry != 0)
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "carry_over",
                Status = "confirmed",
                Id = "carry-" + latestMonthKey,
                Amount = carry,
                Note = Loc.T("Previous_Balance"),
                CreatedAt = now
            });
        }

        // ✅ 2. الدفعات — كل الشهور (تواريخها الحقيقية)
        foreach (var monthKey in allMonths)
        {
            if (!building.Months.TryGetValue(monthKey, out var m)) continue;

            foreach (var d in m.Deposits.Where(d => d.AptId == aptId))
            {
                bool typeMatch = isAll || selectedTypes.Contains("deposits");
                bool statusMatch = true;

                if (!isAll)
                {
                    var hasStatusFilter = selectedTypes.Contains("success")
                                       || selectedTypes.Contains("failed")
                                       || selectedTypes.Contains("pending");
                    if (hasStatusFilter)
                    {
                        statusMatch =
                            (selectedTypes.Contains("success") && d.Status == "confirmed") ||
                            (selectedTypes.Contains("failed") && d.Status == "cancelled") ||
                            (selectedTypes.Contains("pending") && d.Status == "pending");
                    }
                }

                if (typeMatch && statusMatch)
                {
                    txs.Add(new WalletTransactionVm
                    {
                        Type = "deposit",
                        Status = d.Status,
                        Id = d.Id,
                        Number = d.Number,
                        Amount = d.Amount,
                        Note = d.Note,
                        CreatedAt = d.CreatedAt,
                        ReceiptUrl = d.Receipt?.Success == true ? d.Receipt.Url : null
                    });
                }
            }
        }

        // ✅ 3. الرسم الشهري — الشهر الحالي فقط
        if (!apt.Closed && monthlyFee > 0 &&
            (isAll || selectedTypes.Contains("monthly_fee")))
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "monthly_fee_due",
                Status = "confirmed",
                Id = "fee-" + latestMonthKey,
                Amount = -monthlyFee,
                Note = Loc.T("Monthly_Fee_Due"),
                CreatedAt = now
            });
        }

        // ✅ 4. المصاريف — الشهر الحالي فقط
        if ((isAll || selectedTypes.Contains("expenses")) &&
            latestMonth.Distribution.TryGetValue(aptId, out var dist) && dist > 0)
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "expense",
                Status = "confirmed",
                Id = "expense-" + latestMonthKey,
                Amount = -dist,
                Note = Loc.T("Your_Share_Of_This_Month_S"),
                CreatedAt = now
            });
        }

        // ✅ 5. الإيرادات — الشهر الحالي فقط
        if ((isAll || selectedTypes.Contains("revenues")) &&
            latestMonth.RevenueDistribution.TryGetValue(aptId, out var revDist) && revDist > 0)
        {
            txs.Add(new WalletTransactionVm
            {
                Type = "revenue",
                Status = "confirmed",
                Id = "revenue-" + latestMonthKey,
                Amount = revDist,
                Note = Loc.T("Your_Share_Of_The_Building_S"),
                CreatedAt = now
            });
        }

        // ✅ 6. التسويات — كل الشهور (تواريخها الحقيقية)
        foreach (var monthKey in allMonths)
        {
            if (!building.Months.TryGetValue(monthKey, out var m)) continue;

            foreach (var adj in building.WalletAdjustments.Where(a => a.AptId == aptId && a.MonthKey == monthKey))
            {
                if (!isAll && !selectedTypes.Contains("adjustments")) continue;

                txs.Add(new WalletTransactionVm
                {
                    Type = "adjustment",
                    Status = "confirmed",
                    Id = adj.Id,
                    Amount = adj.Amount,
                    Note = adj.Reason,
                    CreatedAt = adj.CreatedAt
                });
            }
        }

        return OrderTransactions(txs);
    }

    // ============================================================
    // ✅ Helper: ترتيب موحّد للحركات
    // ============================================================
    private static List<WalletTransactionVm> OrderTransactions(List<WalletTransactionVm> txs)
    {
        return txs
            .OrderByDescending(t => t.CreatedAt)
            .ThenBy(t => t.Type == "carry_over" ? 0 :
                         t.Type == "monthly_fee_due" ? 1 :
                         t.Type == "revenue" ? 2 :
                         t.Type == "expense" ? 3 : 4)
            .ToList();
    }

    // ============================================================
    // ✅ Building-level totals
    // ============================================================
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

    // ============================================================
    // ✅ كل الدفعات المعلقة من كل الشهور
    // ============================================================
    public List<(Apartment Apt, Deposit Deposit, string MonthKey)> GetAllPendingDepositsCrossMonth(
        Building building)
    {
        var list = new List<(Apartment Apt, Deposit Deposit, string MonthKey)>();

        if (building.Months == null) return list;

        foreach (var kv in building.Months.OrderByDescending(x => x.Key))
        {
            var monthKey = kv.Key;
            var m = kv.Value;

            foreach (var d in m.Deposits.Where(d => d.Status == "pending"))
            {
                var apt = building.Apartments.FirstOrDefault(a => a.Id == d.AptId);
                if (apt != null)
                    list.Add((apt, d, monthKey));
            }
        }

        return list
            .OrderByDescending(x => x.Deposit.CreatedAt)
            .ToList();
    }

    // ============================================================
    // ✅ Helper: جلب آخر شهر موجود قبل الشهر الحالي
    // ============================================================
    private string? GetPreviousMonthKey(Building building, string currentMonthKey)
    {
        if (string.IsNullOrWhiteSpace(currentMonthKey)) return null;

        string? previousKey = null;
        foreach (var key in building.Months.Keys)
        {
            if (string.Compare(key, currentMonthKey, StringComparison.Ordinal) < 0)
            {
                if (previousKey == null || string.Compare(key, previousKey, StringComparison.Ordinal) > 0)
                    previousKey = key;
            }
        }

        return previousKey;
    }

    // ============================================================
    // ✅ حساب الرصيد المُرحّل للشهر الجديد
    // ============================================================
    public Dictionary<string, double> ComputeCarryOverForNextMonth(
        Building building,
        string previousMonthKey)
    {
        var result = new Dictionary<string, double>();

        if (!building.Months.TryGetValue(previousMonthKey, out var prevMonth))
        {
            foreach (var apt in building.Apartments)
                result[apt.Id] = 0;
            return result;
        }

        foreach (var apt in building.Apartments)
        {
            var prevBalance = ComputeWalletBalance(building, apt.Id, previousMonthKey);
            result[apt.Id] = prevBalance;
        }

        return result;
    }

    // ============================================================
    // ✅ إعادة حساب carryOver من شهر معين لحد النهاية
    // ============================================================
    public void RecomputeCarryOverFrom(Building building, string fromMonthKey)
    {
        if (building.Months == null) return;

        var monthsToRecompute = building.Months.Keys
            .Where(k => string.Compare(k, fromMonthKey, StringComparison.Ordinal) >= 0)
            .OrderBy(k => k)
            .ToList();

        foreach (var monthKey in monthsToRecompute)
        {
            var prevMonthKey = GetPreviousMonthKey(building, monthKey);
            var m = building.Months[monthKey];

            if (prevMonthKey != null)
            {
                m.CarryOver = ComputeCarryOverForNextMonth(building, prevMonthKey);
            }
            else
            {
                m.CarryOver = building.Apartments.ToDictionary(a => a.Id, a => 0.0);
            }
        }
    }
}