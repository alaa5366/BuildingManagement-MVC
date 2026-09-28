using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Services;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/views/resident/resident-polls.js
[Authorize(Roles = "resident")]
public class ResidentPollsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly PollsService _polls;
    private readonly AuditLogService _audit;

    public ResidentPollsController(BuildingsService buildings, PollsService polls, AuditLogService audit)
    {
        _buildings = buildings; _polls = polls; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;
    private string ApartmentId => User.FindFirstValue("apartmentId")!;

    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        var allPolls = _polls.GetPolls(building).Where(p => !p.Closed).ToList();
        var paged = BuildingManagementMvc.Models.PagedResult<BuildingManagementMvc.Models.Poll>
            .Create(allPolls, page, pageSize);

        ViewBag.Building = building;
        ViewBag.Apt = apt;
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString()
        };

        return View(paged);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote(string pollId, string optionId)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        var apt = building.Apartments.FirstOrDefault(a => a.Id == ApartmentId);
        if (apt == null) return NotFound();

        if (_polls.Vote(building, pollId, apt.Id, optionId))
        {
            var poll = building.Polls.First(p => p.Id == pollId);
            var optText = poll.Options.First(o => o.Id == optionId).Text;
            _audit.Push(building, "poll_vote", $"{poll.Question} — {optText}", "resident", User.Identity?.Name ?? "resident", apt.Number.ToString());
            await _buildings.SaveFullAsync(building);
        }
        else
        {
            TempData["Error"] = "التصويت مقفول أو أنت صوّت بالفعل";
        }

        return RedirectToAction("Index");
    }
}
