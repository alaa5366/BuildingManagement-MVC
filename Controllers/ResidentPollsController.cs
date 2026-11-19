using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "resident")]
public class ResidentPollsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly TargetPollsService _polls;
    private readonly ILogger<ResidentPollsController> _logger;

    public ResidentPollsController(BuildingsService buildings, TargetPollsService polls, ILogger<ResidentPollsController> logger)
    {
        _buildings = buildings;
        _polls = polls;
        _logger = logger;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;
    private string ApartmentId => User.FindFirstValue("apartmentId")!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return Forbid();

        var paged = await _polls.GetPageAsync(BuildingId, page, pageSize, activeOnly: true, HttpContext.RequestAborted);
        ViewBag.Building = building;
        ViewBag.Apt = apt;
        ViewBag.RouteValues = new Dictionary<string, string?> { ["pageSize"] = paged.PageSize.ToString() };
        return View(paged);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote(string pollId, string optionId)
    {
        if (string.IsNullOrWhiteSpace(ApartmentId)) return Forbid();
        var voted = await _polls.VoteAsync(BuildingId, pollId, ApartmentId, optionId, HttpContext.RequestAborted);
        if (voted)
            _logger.LogInformation("Apartment {ApartmentId} voted in poll {PollId} for building {BuildingId}.",
                ApartmentId, pollId, BuildingId);
        else
            TempData["Error"] = Loc.T("The_Poll_Is_Closed_Or_You");
        return RedirectToAction(nameof(Index));
    }
}
