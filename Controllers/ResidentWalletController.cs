using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/views/resident/resident-wallet.js
[Authorize(Roles = "resident")]
public class ResidentWalletController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly AuditLogService _audit;

    public ResidentWalletController(BuildingsService buildings, WalletService wallet, AuditLogService audit)
    {
        _buildings = buildings; _wallet = wallet; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;
    private string ApartmentId => User.FindFirstValue("apartmentId")!;

    [HttpGet]
    public async Task<IActionResult> Index(string? month)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;
        _wallet.GetOrCreateMonth(building, mk);

        ViewBag.Building = building;
        ViewBag.Apartment = apt;
        ViewBag.Month = mk;
        ViewBag.Balance = _wallet.ComputeWalletBalance(building, apt.Id, mk);
        ViewBag.Transactions = _wallet.GetUnifiedTransactions(building, apt.Id, mk);
        ViewBag.PendingDeposits = _wallet.GetApartmentDeposits(building, apt.Id, mk).Where(d => d.Status == "pending").ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDeposit(string month, double amount, string? note)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return NotFound();

        var mk = string.IsNullOrWhiteSpace(month) ? WalletService.CurrentMonthKey() : month;

        if (amount > 0)
        {
            var d = _wallet.CreateDeposit(building, apt.Id, amount, note, "resident", mk);
            _audit.Push(building, "deposit_create", $"دفعة جديدة {d.Number} — {amount:0.##} ج.م", "resident",
                User.Identity?.Name ?? "resident", apt.Number.ToString());
            await _buildings.SaveFullAsync(building);
            TempData["Message"] = "تم إرسال الدفعة، في انتظار تأكيد الأدمن";
        }

        return RedirectToAction("Index", new { month = mk });
    }
}
