using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Read-only expense and revenue screens backed exclusively by posted target ledger data.</summary>
public sealed class TargetFinanceReadService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public TargetFinanceReadService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>Gets posted financial activity for one building-local financial month.</summary>
    public async Task<TargetFinanceMonthSnapshot> GetMonthAsync(
        string buildingId, string monthKey, string kind, CancellationToken cancellationToken = default)
    {
        ValidateRequest(buildingId, monthKey);
        if (kind is not ("expense" or "revenue"))
            throw new ArgumentOutOfRangeException(nameof(kind));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var categories = await db.FinancialCategories.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.Kind == kind)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new FinancialCategory
            {
                Id = x.Id, Name = x.Name, Color = x.Color,
                Active = x.IsActive, Order = x.SortOrder
            })
            .ToListAsync(cancellationToken);

        var periodIds = await db.FinancialPeriods.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.PeriodKey == monthKey)
            .Select(x => x.PeriodId)
            .ToListAsync(cancellationToken);
        if (periodIds.Count == 0)
            return Empty(categories);

        var transactions = await db.LedgerTransactions.AsNoTracking()
            .Where(x => x.BuildingId == buildingId &&
                periodIds.Contains(x.PeriodId) &&
                x.Status == LedgerTransactionStatus.Posted)
            .ToListAsync(cancellationToken);
        var reversalIds = transactions
            .Where(x => x.Kind == LedgerTransactionKind.Reversal && x.ReversesTransactionId.HasValue)
            .Select(x => x.ReversesTransactionId!.Value)
            .Distinct()
            .ToList();
        var originals = reversalIds.Count == 0
            ? new List<LedgerTransactionEntity>()
            : await db.LedgerTransactions.AsNoTracking()
                .Where(x => x.BuildingId == buildingId && reversalIds.Contains(x.LedgerTransactionId))
                .ToListAsync(cancellationToken);
        var originalById = originals.ToDictionary(x => x.LedgerTransactionId);

        var relevant = transactions.Where(transaction =>
        {
            var sourceKind = transaction.Kind == LedgerTransactionKind.Reversal &&
                transaction.ReversesTransactionId.HasValue &&
                originalById.TryGetValue(transaction.ReversesTransactionId.Value, out var original)
                    ? original.Kind
                    : transaction.Kind;
            return kind == "expense"
                ? sourceKind == LedgerTransactionKind.Expense
                : sourceKind == LedgerTransactionKind.Revenue;
        }).ToList();

        if (relevant.Count == 0)
            return Empty(categories);

        var transactionIds = relevant.Select(x => x.LedgerTransactionId).ToList();
        var entries = await db.LedgerEntries.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && transactionIds.Contains(x.LedgerTransactionId))
            .ToListAsync(cancellationToken);
        var entriesByTransaction = entries.GroupBy(x => x.LedgerTransactionId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var expenses = new List<Expense>();
        var revenues = new List<Revenue>();
        foreach (var transaction in relevant)
        {
            if (!entriesByTransaction.TryGetValue(transaction.LedgerTransactionId, out var transactionEntries))
                continue;

            var sourceKind = transaction.Kind == LedgerTransactionKind.Reversal &&
                transaction.ReversesTransactionId.HasValue &&
                originalById.TryGetValue(transaction.ReversesTransactionId.Value, out var original)
                    ? original.Kind
                    : transaction.Kind;
            var signedTotal = transactionEntries.Sum(x => x.SignedAmount);
            var amount = sourceKind == LedgerTransactionKind.Expense ? -signedTotal : signedTotal;
            if (amount == 0m) continue;

            var date = transactionEntries.Min(x => x.EffectiveAtUtc).ToString(
                "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var note = transaction.Reason ?? string.Empty;
            var id = transaction.SourceKey ?? transaction.LedgerTransactionId.ToString("N");

            // Category identity is not stored on the target ledger transaction or its
            // entries. Keep the row explicitly unclassified instead of querying a retired
            // Expenses/Revenues table or fabricating a category association.
            if (kind == "expense")
                expenses.Add(new Expense { Id = id, CategoryId = "", Note = note, Amount = amount, Date = date });
            else
                revenues.Add(new Revenue { Id = id, CategoryId = "", Note = note, Amount = amount, Date = date });
        }

        expenses = expenses.OrderByDescending(x => x.Date, StringComparer.Ordinal)
            .ThenByDescending(x => x.Id, StringComparer.Ordinal).ToList();
        revenues = revenues.OrderByDescending(x => x.Date, StringComparer.Ordinal)
            .ThenByDescending(x => x.Id, StringComparer.Ordinal).ToList();
        var total = kind == "expense" ? expenses.Sum(x => x.Amount) : revenues.Sum(x => x.Amount);
        return new TargetFinanceMonthSnapshot(categories, expenses, revenues, total);
    }

    /// <summary>
    /// Gets net posted expense totals by category for the requested periods.
    /// The target ledger currently has no category foreign key, so unclassified
    /// amounts are returned under the empty category key.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, decimal>> GetExpenseCategoryTotalsAsync(
        string buildingId,
        IReadOnlyCollection<string> monthKeys,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building is required.", nameof(buildingId));
        ArgumentNullException.ThrowIfNull(monthKeys);
        if (monthKeys.Count > 600)
            throw new ArgumentOutOfRangeException(nameof(monthKeys), "At most 600 report months can be queried.");
        foreach (var monthKey in monthKeys)
            ValidateMonth(monthKey);

        var keys = monthKeys.Distinct(StringComparer.Ordinal).ToList();
        if (keys.Count == 0)
            return new Dictionary<string, decimal>(StringComparer.Ordinal);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var periodIds = await db.FinancialPeriods.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && keys.Contains(x.PeriodKey))
            .Select(x => x.PeriodId)
            .ToListAsync(cancellationToken);
        if (periodIds.Count == 0)
            return new Dictionary<string, decimal>(StringComparer.Ordinal);

        var transactions = await db.LedgerTransactions.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && periodIds.Contains(x.PeriodId) &&
                x.Status == LedgerTransactionStatus.Posted)
            .ToListAsync(cancellationToken);
        var reversals = transactions.Where(x => x.Kind == LedgerTransactionKind.Reversal &&
                x.ReversesTransactionId.HasValue)
            .ToList();
        var originalIds = reversals.Select(x => x.ReversesTransactionId!.Value).Distinct().ToList();
        var originals = originalIds.Count == 0
            ? new List<LedgerTransactionEntity>()
            : await db.LedgerTransactions.AsNoTracking()
                .Where(x => x.BuildingId == buildingId && originalIds.Contains(x.LedgerTransactionId))
                .ToListAsync(cancellationToken);
        var originalById = originals.ToDictionary(x => x.LedgerTransactionId);
        var expenseTransactionIds = transactions.Where(x =>
                x.Kind == LedgerTransactionKind.Expense ||
                x.Kind == LedgerTransactionKind.Reversal && x.ReversesTransactionId.HasValue &&
                originalById.TryGetValue(x.ReversesTransactionId.Value, out var original) &&
                original.Kind == LedgerTransactionKind.Expense)
            .Select(x => x.LedgerTransactionId).ToList();
        if (expenseTransactionIds.Count == 0)
            return new Dictionary<string, decimal>(StringComparer.Ordinal);

        var netExpense = await db.LedgerEntries.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && expenseTransactionIds.Contains(x.LedgerTransactionId))
            .SumAsync(x => (decimal?)-x.SignedAmount, cancellationToken) ?? 0m;
        return netExpense == 0m
            ? new Dictionary<string, decimal>(StringComparer.Ordinal)
            : new Dictionary<string, decimal>(StringComparer.Ordinal) { [""] = netExpense };
    }

    private static TargetFinanceMonthSnapshot Empty(IReadOnlyList<FinancialCategory> categories) =>
        new(categories, Array.Empty<Expense>(), Array.Empty<Revenue>(), 0m);

    private static void ValidateRequest(string buildingId, string monthKey)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building is required.", nameof(buildingId));
        ValidateMonth(monthKey);
    }

    private static void ValidateMonth(string monthKey)
    {
        if (!DateOnly.TryParseExact(monthKey + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _))
            throw new ArgumentException("Month must use yyyy-MM format.", nameof(monthKey));
    }
}

/// <summary>Represents the finance snapshot for a building and financial month.</summary>
public sealed record TargetFinanceMonthSnapshot(
    IReadOnlyList<FinancialCategory> Categories,
    IReadOnlyList<Expense> Expenses,
    IReadOnlyList<Revenue> Revenues,
    decimal Total);
