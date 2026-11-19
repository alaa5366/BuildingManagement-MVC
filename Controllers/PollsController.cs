using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ManagePolls)]
public class PollsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly TargetPollsService _polls;
    private readonly ILogger<PollsController> _logger;

    public PollsController(BuildingsService buildings, TargetPollsService polls, ILogger<PollsController> logger)
    {
        _buildings = buildings;
        _polls = polls;
        _logger = logger;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var paged = await _polls.GetPageAsync(BuildingId, page, pageSize, activeOnly: false, HttpContext.RequestAborted);
        ViewBag.Building = building;
        ViewBag.RouteValues = new Dictionary<string, string?> { ["pageSize"] = paged.PageSize.ToString() };
        return View(paged);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string question, string? description, string? deadline, List<string> options)
    {
        try
        {
            var poll = await _polls.CreateAsync(BuildingId, question, description, options ?? new(),
                deadline, User.Identity?.Name ?? "admin", HttpContext.RequestAborted);
            _logger.LogInformation("Poll {PollId} created for building {BuildingId} by {Actor}.",
                poll.Id, BuildingId, User.Identity?.Name);
            TempData["Message"] = Loc.T("Poll_Published");
        }
        catch (ArgumentException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(string id)
    {
        if (await _polls.SetClosedAsync(BuildingId, id, true, HttpContext.RequestAborted))
            _logger.LogInformation("Poll {PollId} closed for building {BuildingId}.", id, BuildingId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(string id)
    {
        if (await _polls.SetClosedAsync(BuildingId, id, false, HttpContext.RequestAborted))
            _logger.LogInformation("Poll {PollId} reopened for building {BuildingId}.", id, BuildingId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (await _polls.DeleteAsync(BuildingId, id, HttpContext.RequestAborted))
            _logger.LogInformation("Poll {PollId} deleted for building {BuildingId}.", id, BuildingId);
        return RedirectToAction(nameof(Index));
    }
}
