using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.ViewModels;

/// <summary>
/// Model of the account statement partial view (page of transactions plus route values).
/// </summary>
public class StatementPartialViewModels
{
    /// <summary>
    /// Gets or sets the transactions.
    /// </summary>
    public PagedResult<WalletTransactionVm> Transactions { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether it is all months.
    /// </summary>
    public bool AllMonths { get; set; }
    /// <summary>
    /// Gets or sets the route values.
    /// </summary>
    public Dictionary<string, string?> RouteValues { get; set; } = new();
}