using System.Globalization;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Shared validation rules for new legacy-aggregate finance commands.
/// These checks do not rewrite or normalize historical records.
/// </summary>
public static class FinanceCommandValidation
{
    public static void ValidateMonthKey(string monthKey)
    {
        if (string.IsNullOrWhiteSpace(monthKey) ||
            !DateOnly.TryParseExact(monthKey, "yyyy-MM", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
            throw new ArgumentException("Month must use the exact yyyy-MM format.", nameof(monthKey));
    }

    public static void ValidatePositiveMoney(decimal amount, string parameterName)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "Amount must be greater than zero.");
        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
            throw new ArgumentOutOfRangeException(parameterName, "Amount cannot contain fractions smaller than one cent.");
    }

    public static string ResolveTransactionDate(string? date, string monthKey)
    {
        ValidateMonthKey(monthKey);
        var resolved = string.IsNullOrWhiteSpace(date)
            ? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : date.Trim();

        if (!DateOnly.TryParseExact(resolved, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            throw new ArgumentException("Transaction date must use the exact yyyy-MM-dd format.", nameof(date));

        if (parsed.ToString("yyyy-MM", CultureInfo.InvariantCulture) != monthKey)
            throw new ArgumentException("Transaction date must belong to the selected month.", nameof(date));

        return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static void ValidateActiveCategory(IEnumerable<FinancialCategory> categories, string categoryId, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(categoryId) ||
            !categories.Any(c => c.Active && string.Equals(c.Id, categoryId, StringComparison.Ordinal)))
            throw new ArgumentException("A valid active financial category is required.", parameterName);
    }

    public static void ValidateActor(string actor, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("A recorded actor is required.", parameterName);
    }
}
