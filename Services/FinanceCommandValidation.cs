using System.Globalization;
using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Shared validation rules for new legacy-aggregate finance commands.
/// These checks do not rewrite or normalize historical records.
/// </summary>
public static class FinanceCommandValidation
{
    /// <summary>Validates that a month key uses the exact yyyy-MM format.</summary>
    /// <param name="monthKey">The month key to validate.</param>
    /// <exception cref="ArgumentException">The key is missing or invalid.</exception>
    public static void ValidateMonthKey(string monthKey)
    {
        if (string.IsNullOrWhiteSpace(monthKey) ||
            !DateOnly.TryParseExact(monthKey + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
            throw new ArgumentException("Month must use the exact yyyy-MM format.", nameof(monthKey));
    }

    /// <summary>Validates that a monetary amount is positive and precise to at most two decimal places.</summary>
    /// <param name="amount">The amount to validate.</param>
    /// <param name="parameterName">The name used when reporting an invalid argument.</param>
    /// <exception cref="ArgumentOutOfRangeException">The amount is non-positive or contains fractions smaller than one cent.</exception>
    public static void ValidatePositiveMoney(decimal amount, string parameterName)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "Amount must be greater than zero.");
        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
            throw new ArgumentOutOfRangeException(parameterName, "Amount cannot contain fractions smaller than one cent.");
    }

    /// <summary>Resolves and validates a transaction date within the selected month.</summary>
    /// <param name="date">The optional transaction date in yyyy-MM-dd format.</param>
    /// <param name="monthKey">The selected month in yyyy-MM format.</param>
    /// <returns>The validated transaction date in yyyy-MM-dd format.</returns>
    /// <exception cref="ArgumentException">The month or transaction date is invalid or the date belongs to another month.</exception>
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

    /// <summary>Ensures the requested financial category exists and is active.</summary>
    /// <param name="categories">The available financial categories.</param>
    /// <param name="categoryId">The category identifier to validate.</param>
    /// <param name="parameterName">The name used when reporting an invalid argument.</param>
    /// <exception cref="ArgumentException">No matching active category exists.</exception>
    public static void ValidateActiveCategory(IEnumerable<FinancialCategory> categories, string categoryId, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(categoryId) ||
            !categories.Any(c => c.Active && string.Equals(c.Id, categoryId, StringComparison.Ordinal)))
            throw new ArgumentException("A valid active financial category is required.", parameterName);
    }

    /// <summary>Ensures a non-empty actor identity is recorded for the finance command.</summary>
    /// <param name="actor">The actor identity to validate.</param>
    /// <param name="parameterName">The name used when reporting an invalid argument.</param>
    /// <exception cref="ArgumentException">The actor identity is missing.</exception>
    public static void ValidateActor(string actor, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("A recorded actor is required.", parameterName);
    }
}
