using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/features/reports.js
[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ViewReports)]
public class ReportsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly ReportsService _reports;
    private readonly WalletService _wallet;
    private readonly AuditLogService _audit;

    public ReportsController(BuildingsService buildings, ReportsService reports, WalletService wallet, AuditLogService audit)
    {
        _buildings = buildings; _reports = reports; _wallet = wallet; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index(string? range)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var r = string.IsNullOrWhiteSpace(range) ? "6m" : range;
        var vm = _reports.BuildReport(building, r, WalletService.CurrentMonthKey());
        return View(vm);
    }

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
        void Row(params string[] cells) => sb.AppendLine(string.Join(",", cells.Select(c => $"\"{(c ?? "").Replace("\"", "\"\"")}\"")));

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

        _audit.Push(building, "report_export", "CSV", "admin", User.Identity?.Name ?? "admin");
        await _buildings.SaveFullAsync(building);

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"report-{building.BuildingNumber}-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
