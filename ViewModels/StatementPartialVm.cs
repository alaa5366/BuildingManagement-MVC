using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.ViewModels;

public class StatementPartialVm
{
    public PagedResult<WalletTransactionVm> Transactions { get; set; } = new();
    public bool AllMonths { get; set; }
    public Dictionary<string, string?> RouteValues { get; set; } = new();
}