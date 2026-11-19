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
    private readonly ReportsService _reports;
    private readonly IAuditLogger _audit;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsController"/> class.
    /// </summary>
    public ReportsController(ReportsService reports, IAuditLogger audit)
    {
        _reports = reports;
        _audit = audit;
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
        var selectedRange = string.IsNullOrWhiteSpace(range) ? "6m" : range;
        var currentMonth = WalletService.CurrentMonthKey();
        var vm = await _reports.BuildReportAsync(BuildingId, selectedRange, currentMonth, HttpContext.RequestAborted);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? range)
    {
        var selectedRange = string.IsNullOrWhiteSpace(range) ? "6m" : range;
        var currentMonth = WalletService.CurrentMonthKey();
        var vm = await _reports.BuildReportAsync(BuildingId, selectedRange, currentMonth, HttpContext.RequestAborted);
        var building = vm.Building;

        var sb = new StringBuilder();
        sb.Append('\\uFEFF');
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
        foreach (var point in vm.Monthly)
            Row(point.Month, point.Collected.ToString("0.##"), point.Revenues.ToString("0.##"),
                point.Expenses.ToString("0.##"), (point.Collected + point.Revenues - point.Expenses).ToString("0.##"));

        Row("");
        Row(Loc.T("Wallet_Distribution_Current_Month_2"));
        Row(Loc.T("Apartment"), Loc.T("Owner"), Loc.T("Balance"));
        foreach (var point in vm.Wallet)
        {
            var apartment = building.Apartments.FirstOrDefault(item => item.Number == point.Number);
            Row(point.Number.ToString(), apartment?.Owner ?? "", point.Balance.ToString("0.##"));
        }

        await _audit.LogAsync("report_export", building.Id, userId: User.Identity?.Name ?? "admin",
            userRole: "admin", metadata: new { format = "CSV", range = selectedRange });

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"report-{building.BuildingNumber}-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
