﻿using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  SuperAdminHomeController	لوحة SuperAdmin الرئيسية
// ============================================================
/// <summary>
/// Super admin dashboard with the buildings overview.
/// </summary>
/// <remarks>
/// Access: requires one of the roles <c>superadmin</c>.
/// </remarks>
[Authorize(Roles = "superadmin")]
public class SuperAdminHomeController : Controller
{
    private readonly BuildingsService _buildings;
    /// <summary>
    /// Initializes a new instance of the <see cref="SuperAdminHomeController"/> class.
    /// </summary>
    public SuperAdminHomeController(BuildingsService buildings) => _buildings = buildings;

    /// <summary>
    /// Shows the main page of the super admin home screen.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var buildings = await _buildings.GetSummariesWithCountsAsync();
        return View(buildings);
    }
}
