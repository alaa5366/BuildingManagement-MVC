using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Read-only expense and revenue screens backed exclusively by target finance tables.</summary>
public sealed class TargetFinanceReadService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public TargetFinanceReadService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>Gets financial totals for the requested month.</summary>
    public async Task<TargetFinanceMonthSnapshot> GetMonthAsync(
        string buildingId, string monthKey, string kind, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId)) throw new ArgumentException("Building is required.", nameof(buildingId));
        if (!DateOnly.TryParseExact(monthKey + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
            throw new ArgumentException("Month must use yyyy-MM format.", nameof(monthKey));
        if (kind is not ("expense" or "revenue")) throw new ArgumentOutOfRangeException(nameof(kind));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var categories = await db.FinancialCategories.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.Kind == kind)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new FinancialCategory { Id = x.Id, Name = x.Name, Color = x.Color, Active = x.IsActive, Order = x.SortOrder })
            .ToListAsync(cancellationToken);

        if (kind == "expense")
        {
            var reversedIds = await GetReversedSourceIdsAsync(db, buildingId, "Expense", cancellationToken);
            var rows = await db.Expenses.AsNoTracking()
                .Where(x => x.BuildingId == buildingId && x.MonthKey == monthKey)
                .OrderByDescending(x => x.ExpenseDate).ThenByDescending(x => x.Id)
                .Select(x => new { x.Id, x.CategoryId, x.Note, x.Amount, x.ExpenseDate })
                .ToListAsync(cancellationToken);
            var items = rows.Where(x => !reversedIds.Contains(x.Id)).Select(x => new Expense
            {
                Id = x.Id, CategoryId = x.CategoryId ?? "", Note = x.Note,
                Amount = x.Amount, Date = x.ExpenseDate.ToString("yyyy-MM-dd")
            }).ToList();
            return new TargetFinanceMonthSnapshot(categories, items, Array.Empty<Revenue>(), items.Sum(x => x.Amount));
        }

        var reversedRevenueIds = await GetReversedSourceIdsAsync(db, buildingId, "Revenue", cancellationToken);
        var revenueRows = await db.Revenues.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.MonthKey == monthKey)
            .OrderByDescending(x => x.RevenueDate).ThenByDescending(x => x.Id)
            .Select(x => new { x.Id, x.CategoryId, x.Note, x.Amount, x.RevenueDate })
            .ToListAsync(cancellationToken);
        var revenues = revenueRows.Where(x => !reversedRevenueIds.Contains(x.Id)).Select(x => new Revenue
        {
            Id = x.Id, CategoryId = x.CategoryId ?? "", Note = x.Note,
            Amount = x.Amount, Date = x.RevenueDate.ToString("yyyy-MM-dd")
        }).ToList();
        return new TargetFinanceMonthSnapshot(categories, Array.Empty<Expense>(), revenues, revenues.Sum(x => x.Amount));
    }

    /// <summary>Gets expense totals grouped by category for the requested period.</summary>
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
        {
            if (!DateOnly.TryParseExact(monthKey + "-01", "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _))
                throw new ArgumentException("Month must use yyyy-MM format.", nameof(monthKeys));
        }

        var keys = monthKeys.Distinct(StringComparer.Ordinal).ToList();
        if (keys.Count == 0)
            return new Dictionary<string, decimal>(StringComparer.Ordinal);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var periodIds = await db.FinancialPeriods.AsNoTracking()
            .Where(period => period.BuildingId == buildingId && keys.Contains(period.PeriodKey))
            .Select(period => period.PeriodId)
            .ToListAsync(cancellationToken);
        var transactions = await db.LedgerTransactions.AsNoTracking()
            .Where(transaction => transaction.BuildingId == buildingId &&
                periodIds.Contains(transaction.PeriodId) &&
                transaction.Status == LedgerTransactionStatus.Posted &&
                (transaction.SourceType == "Expense" && transaction.Kind == LedgerTransactionKind.Expense ||
                 transaction.SourceType == "ExpenseReversal" && transaction.Kind == LedgerTransactionKind.Reversal))
            .Select(transaction => new { transaction.SourceType, transaction.SourceKey })
            .ToListAsync(cancellationToken);
        var expenses = await db.Expenses.AsNoTracking()
            .Where(expense => expense.BuildingId == buildingId)
            .Select(expense => new { expense.Id, expense.CategoryId, expense.Amount })
            .ToListAsync(cancellationToken);
        var expenseById = expenses.ToDictionary(
            expense => NormalizeSourceKey(expense.Id), StringComparer.OrdinalIgnoreCase);

        var totals = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var transaction in transactions)
        {
            var sourceId = NormalizeSourceKey(transaction.SourceKey);
            if (string.IsNullOrWhiteSpace(sourceId) || !expenseById.TryGetValue(sourceId, out var expense))
                continue;
            var categoryId = expense.CategoryId ?? "";
            var amount = transaction.SourceType == "ExpenseReversal"
                ? -expense.Amount
                : expense.Amount;
            totals[categoryId] = totals.GetValueOrDefault(categoryId) + amount;
        }

        return totals;
    }

    private static string NormalizeSourceKey(string? key) =>
        Guid.TryParse(key, out var id) ? id.ToString("N") : key ?? "";

    private static async Task<HashSet<string>> GetReversedSourceIdsAsync(
        AppDbContext db, string buildingId, string sourceType, CancellationToken cancellationToken)
    {
        var sourceKeys = await (
            from original in db.LedgerTransactions.AsNoTracking()
            join reversal in db.LedgerTransactions.AsNoTracking()
                on original.LedgerTransactionId equals reversal.ReversesTransactionId
            where original.BuildingId == buildingId && original.SourceSystem == "application" &&
                  original.SourceType == sourceType && original.Status == LedgerTransactionStatus.Posted &&
                  reversal.BuildingId == buildingId && reversal.Kind == LedgerTransactionKind.Reversal &&
                  reversal.Status == LedgerTransactionStatus.Posted
            select original.SourceKey)
            .ToListAsync(cancellationToken);

        return sourceKeys.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Guid.TryParse(x, out var id) ? id.ToString("N") : x!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record TargetFinanceMonthSnapshot(
    IReadOnlyList<FinancialCategory> Categories,
    IReadOnlyList<Expense> Expenses,
    IReadOnlyList<Revenue> Revenues,
    decimal Total);
