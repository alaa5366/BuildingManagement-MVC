namespace BuildingManagementMvc.Data;

/// <summary>
/// Deterministic equal-weight allocation using the largest-remainder method.
/// Ties are resolved by ordinal ApartmentId so retries produce identical results.
/// </summary>
public static class FinancialAllocationCalculator
{
    public const string AlgorithmVersion = "equal-largest-remainder-v1";

    /// <summary>Allocates an amount equally across the supplied targets while preserving the total.</summary>
    public static IReadOnlyList<FinancialAllocationResult> AllocateEqually(
        decimal amount,
        IEnumerable<string> apartmentIds)
    {
        ArgumentNullException.ThrowIfNull(apartmentIds);
        if (amount <= 0m || decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
            throw new ArgumentOutOfRangeException(nameof(amount), "Allocation amount must be positive and precise to cents.");

        var ids = apartmentIds.ToArray();
        if (ids.Length == 0)
            throw new ArgumentException("At least one eligible apartment is required.", nameof(apartmentIds));
        if (ids.Any(string.IsNullOrWhiteSpace) || ids.Any(id => id.Length > 64))
            throw new ArgumentException("Apartment identifiers must be non-empty and at most 64 characters.", nameof(apartmentIds));
        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new ArgumentException("Apartment identifiers must be unique.", nameof(apartmentIds));

        var ordered = ids.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var totalCents = checked((long)(amount * 100m));
        var baseCents = totalCents / ordered.Length;
        var remainderCents = totalCents % ordered.Length;

        return ordered.Select((id, index) =>
        {
            var cents = baseCents + (index < remainderCents ? 1L : 0L);
            return new FinancialAllocationResult(id, 1m, amount / ordered.Length,
                cents / 100m, index + 1);
        }).ToArray();
    }
}

/// <summary>One deterministic apartment share produced by the allocation calculator.</summary>
public sealed record FinancialAllocationResult(
    string ApartmentId,
    decimal Weight,
    decimal ExactShare,
    decimal AllocatedAmount,
    int RemainderRank);
