using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

/// <summary>ResidentPollsController coordinates the corresponding application functionality.</summary>
[Authorize(Roles = "resident")]
public class ResidentPollsController : Controller
{
    private readonly TargetPollsService _polls;
    private readonly ILogger<ResidentPollsController> _logger;

    public ResidentPollsController(TargetPollsService polls, ILogger<ResidentPollsController> logger)
    {
        _polls = polls;
        _logger = logger;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;
    private string ApartmentId => User.FindFirstValue("apartmentId")!;

    /// <summary>Displays the current records and associated management actions.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var summary = await _polls.GetBuildingSummaryAsync(BuildingId, HttpContext.RequestAborted);
        if (summary is null) return NotFound();
        if (!await _polls.IsActiveApartmentAsync(BuildingId, ApartmentId, HttpContext.RequestAborted))
            return Forbid();

        var paged = await _polls.GetPageAsync(BuildingId, page, pageSize, activeOnly: true, HttpContext.RequestAborted, subjectReference: ApartmentId);
        ViewBag.BuildingName = summary.Value.Name;
        ViewBag.OpenApartmentCount = summary.Value.ActiveApartmentCount;
        ViewBag.ApartmentId = ApartmentId;
        ViewBag.RouteValues = new Dictionary<string, string?> { ["pageSize"] = paged.PageSize.ToString() };
        return View(paged);
    }

    /// <summary>Submits the resident's vote for the selected poll.</summary>
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
