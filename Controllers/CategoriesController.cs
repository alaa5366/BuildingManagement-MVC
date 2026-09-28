using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "superadmin,admin")]
[HasPermission(AdminPermissions.ManageCategories)]
public class CategoriesController : Controller
{
    private readonly CategoriesService _categories;
    private readonly BuildingsService _buildings;

    public CategoriesController(CategoriesService categories, BuildingsService buildings)
    {
        _categories = categories;
        _buildings = buildings;
    }

    private string CurrentUserId =>
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";

    private string? CurrentBuildingId =>
        User.FindFirst("buildingId")?.Value;

    private bool IsSuperAdmin => User.IsInRole("superadmin");

    // ============================================================
    // صفحة الفئات
    // ============================================================
    public async Task<IActionResult> Index()
    {
        if (IsSuperAdmin)
        {
            var buildings = await _buildings.GetAllAsync();
            return View("ChooseBuilding", buildings);
        }

        var buildingId = CurrentBuildingId;
        if (string.IsNullOrEmpty(buildingId)) return Forbid();

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        ViewBag.Building = building;
        ViewBag.ExpenseCategories = _categories.GetExpenseCategories(building);
        ViewBag.RevenueCategories = _categories.GetRevenueCategories(building);
        return View();
    }

    // ============================================================
    // صفحة الفئات لعمارة محددة (Super Admin فقط)
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> Manage(string id)
    {
        if (!IsSuperAdmin) return Forbid();

        var building = await _buildings.GetByIdAsync(id);
        if (building == null) return NotFound();

        ViewBag.Building = building;
        ViewBag.ExpenseCategories = _categories.GetExpenseCategories(building);
        ViewBag.RevenueCategories = _categories.GetRevenueCategories(building);
        return View("Index");
    }

    // ============================================================
    // إضافة فئة
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(
        string buildingId, bool isExpense, string name, string color)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "اسم الفئة مطلوب.";
            return RedirectBack(buildingId);
        }

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();

        if (!CanAccessBuilding(buildingId)) return Forbid();

        await _categories.AddAsync(building, isExpense, name, color, CurrentUserId);
        TempData["Message"] = "✅ تمت إضافة الفئة.";
        return RedirectBack(buildingId);
    }

    // ============================================================
    // تعديل فئة
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        string buildingId, bool isExpense, string id,
        string name, string color, bool active)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();
        if (!CanAccessBuilding(buildingId)) return Forbid();

        var ok = await _categories.UpdateAsync(building, isExpense, id, name, color, active, CurrentUserId);
        TempData[ok ? "Message" : "Error"] = ok ? "✅ تم التحديث." : "❌ الفئة غير موجودة.";
        return RedirectBack(buildingId);
    }

    // ============================================================
    // حذف / تعطيل فئة
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string buildingId, bool isExpense, string id)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();
        if (!CanAccessBuilding(buildingId)) return Forbid();

        var ok = await _categories.DeleteAsync(building, isExpense, id, CurrentUserId);
        TempData[ok ? "Message" : "Error"] = ok
            ? "✅ تم الحذف (أو التعطيل لو مستخدمة)."
            : "❌ الفئة غير موجودة.";
        return RedirectBack(buildingId);
    }

    // ============================================================
    // إعادة الترتيب (AJAX)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(string buildingId, bool isExpense, List<string> ids)
    {
        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return NotFound();
        if (!CanAccessBuilding(buildingId)) return Forbid();

        await _categories.ReorderAsync(building, isExpense, ids ?? new(), CurrentUserId);
        return Json(new { success = true });
    }

    // ============================================================
    // Helpers
    // ============================================================
    private bool CanAccessBuilding(string buildingId)
    {
        if (IsSuperAdmin) return true;
        return CurrentBuildingId == buildingId;
    }

    private IActionResult RedirectBack(string buildingId)
    {
        if (IsSuperAdmin)
            return RedirectToAction(nameof(Manage), new { id = buildingId });
        return RedirectToAction(nameof(Index));
    }
}