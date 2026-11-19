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
            .Where(x => x.BuildingId == buildingId && x.Kind == kind && x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new FinancialCategory { Id = x.Id, Name = x.Name, Color = x.Color, Active = x.IsActive, Order = x.SortOrder })
            .ToListAsync(cancellationToken);

        if (kind == "expense")
        {
            var rows = await db.Expenses.AsNoTracking()
                .Where(x => x.BuildingId == buildingId && x.MonthKey == monthKey)
                .OrderByDescending(x => x.ExpenseDate).ThenByDescending(x => x.Id)
                .Select(x => new { x.Id, x.CategoryId, x.Note, x.Amount, x.ExpenseDate })
                .ToListAsync(cancellationToken);
            var items = rows.Select(x => new Expense
            {
                Id = x.Id, CategoryId = x.CategoryId ?? "", Note = x.Note,
                Amount = x.Amount, Date = x.ExpenseDate.ToString("yyyy-MM-dd")
            }).ToList();
            return new TargetFinanceMonthSnapshot(categories, items, Array.Empty<Revenue>(), items.Sum(x => x.Amount));
        }

        var revenueRows = await db.Revenues.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.MonthKey == monthKey)
            .OrderByDescending(x => x.RevenueDate).ThenByDescending(x => x.Id)
            .Select(x => new { x.Id, x.CategoryId, x.Note, x.Amount, x.RevenueDate })
            .ToListAsync(cancellationToken);
        var revenues = revenueRows.Select(x => new Revenue
        {
            Id = x.Id, CategoryId = x.CategoryId ?? "", Note = x.Note,
            Amount = x.Amount, Date = x.RevenueDate.ToString("yyyy-MM-dd")
        }).ToList();
        return new TargetFinanceMonthSnapshot(categories, Array.Empty<Expense>(), revenues, revenues.Sum(x => x.Amount));
    }
}

public sealed record TargetFinanceMonthSnapshot(
    IReadOnlyList<FinancialCategory> Categories,
    IReadOnlyList<Expense> Expenses,
    IReadOnlyList<Revenue> Revenues,
    decimal Total);
