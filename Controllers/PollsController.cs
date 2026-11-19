using BuildingManagementMvc.Attributes;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

/// <summary>PollsController coordinates the corresponding application controller actions.</summary>
[Authorize(Roles = "admin")]
[AdminPermission(BuildingManagementMvc.Models.AdminPermissions.ManagePolls)]
public class PollsController : Controller
{
    private readonly TargetPollsService _polls;
    private readonly ILogger<PollsController> _logger;

    public PollsController(TargetPollsService polls, ILogger<PollsController> logger)
    {
        _polls = polls;
        _logger = logger;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    /// <summary>Displays the current records and associated management actions.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var summary = await _polls.GetBuildingSummaryAsync(BuildingId, HttpContext.RequestAborted);
        if (summary is null) return NotFound();

        var paged = await _polls.GetPageAsync(BuildingId, page, pageSize, activeOnly: false, HttpContext.RequestAborted);
        ViewBag.BuildingName = summary.Value.Name;
        ViewBag.OpenApartmentCount = summary.Value.ActiveApartmentCount;
        ViewBag.RouteValues = new Dictionary<string, string?> { ["pageSize"] = paged.PageSize.ToString() };
        return View(paged);
    }

    /// <summary>Handles the Create operation.</summary>
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

    /// <summary>Handles the Close operation.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(string id)
    {
        if (await _polls.SetClosedAsync(BuildingId, id, true, HttpContext.RequestAborted))
            _logger.LogInformation("Poll {PollId} closed for building {BuildingId}.", id, BuildingId);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Handles the Reopen operation.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(string id)
    {
        if (await _polls.SetClosedAsync(BuildingId, id, false, HttpContext.RequestAborted))
            _logger.LogInformation("Poll {PollId} reopened for building {BuildingId}.", id, BuildingId);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Deletes the specified record.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (await _polls.DeleteAsync(BuildingId, id, HttpContext.RequestAborted))
            _logger.LogInformation("Poll {PollId} deleted for building {BuildingId}.", id, BuildingId);
        return RedirectToAction(nameof(Index));
    }
}
