using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  ReportsController	التقارير المالية (Charts + CSV)
// ============================================================
/// <summary>
/// Financial reports and charts for a building, including CSV export.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>admin</c>.
/// </remarks>
[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ViewReports)]
public class ReportsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly ReportsService _reports;
    private readonly WalletService _wallet;
    private readonly IAuditLogger _audit;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsController"/> class.
    /// </summary>
    public ReportsController(BuildingsService buildings, ReportsService reports, WalletService wallet, IAuditLogger audit)
    {
        _buildings = buildings; _reports = reports; _wallet = wallet; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    /// <summary>
    /// Shows the main page of the reports screen.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Index(string? range)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var r = string.IsNullOrWhiteSpace(range) ? "6m" : range;
        var vm = _reports.BuildReport(building, r, WalletService.CurrentMonthKey());
        return View(vm);
    }

    /// <summary>
    /// Exports the CSV.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? range)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var r = string.IsNullOrWhiteSpace(range) ? "6m" : range;
        var currentMonth = WalletService.CurrentMonthKey();
        var vm = _reports.BuildReport(building, r, currentMonth);

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        void Row(params string[] cells) => sb.AppendLine(string.Join(",", cells.Select(CsvExportSafety.Cell)));

        Row(Loc.T("Report_N_N", building.Name, building.BuildingNumber));
        Row(Loc.T("Export_Date_N", DateTime.UtcNow));
        Row("");
        Row(Loc.T("Summary"));
        Row(Loc.T("Total_Collected"), vm.TotalCollected.ToString("0.##"));
        Row(Loc.T("Total_Revenues"), vm.TotalRevenues.ToString("0.##"));
        Row(Loc.T("Total_Expenses"), vm.TotalExpenses.ToString("0.##"));
        Row(Loc.T("Net_Balance"), vm.NetBalance.ToString("0.##"));
        Row(Loc.T("Monthly_Average"), vm.AvgMonthly.ToString("0.##"));
        Row("");
        Row(Loc.T("Monthly_2"));
        Row(Loc.T("Month"), Loc.T("Collected_2"), Loc.T("Revenues_2"), Loc.T("Expenses"), Loc.T("Net"));
        foreach (var mp in vm.Monthly)
            Row(mp.Month, mp.Collected.ToString("0.##"), mp.Revenues.ToString("0.##"), mp.Expenses.ToString("0.##"),
                (mp.Collected + mp.Revenues - mp.Expenses).ToString("0.##"));

        Row("");
        Row(Loc.T("Wallet_Distribution_Current_Month_2"));
        Row(Loc.T("Apartment"), Loc.T("Owner"), Loc.T("Balance"));
        foreach (var a in building.Apartments)
        {
            var bal = _wallet.ComputeWalletBalance(building, a.Id, currentMonth);
            Row(a.Number.ToString(), a.Owner ?? "", bal.ToString("0.##"));
        }

        await _audit.LogAsync("report_export", building.Id, userId: User.Identity?.Name ?? "admin",
            userRole: "admin", metadata: new { format = "CSV", range = r });

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"report-{building.BuildingNumber}-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
