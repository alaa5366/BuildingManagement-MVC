using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingManagementMvc.Controllers;

// نفس شاشة js/views/admin/admin-polls.js
[Authorize(Roles = "admin")]
public class PollsController : Controller
{
    private readonly BuildingsService _buildings;
    private readonly PollsService _polls;
    private readonly AuditLogService _audit;

    public PollsController(BuildingsService buildings, PollsService polls, AuditLogService audit)
    {
        _buildings = buildings; _polls = polls; _audit = audit;
    }

    private string BuildingId => User.FindFirstValue("buildingId")!;

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var allPolls = _polls.GetPolls(building);
        var paged = BuildingManagementMvc.Models.PagedResult<BuildingManagementMvc.Models.Poll>
            .Create(allPolls, page, pageSize);

        ViewBag.Building = building;
        ViewBag.RouteValues = new Dictionary<string, string?>
        {
            ["pageSize"] = pageSize.ToString()
        };

        return View(paged);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string question, string? description, string? deadline, List<string> options)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();

        var validOptions = (options ?? new()).Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
        if (string.IsNullOrWhiteSpace(question) || validOptions.Count < 2)
        {
            TempData["Error"] = "لازم سؤال + خيارين على الأقل";
            return RedirectToAction("Index");
        }

        var poll = _polls.CreatePoll(building, question.Trim(), description, validOptions, deadline, User.Identity?.Name ?? "admin");
        _audit.Push(building, "poll_create", poll.Question, "admin", User.Identity?.Name ?? "admin");
        await _buildings.SaveFullAsync(building);
        TempData["Message"] = "تم نشر التصويت";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(string id)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        if (_polls.SetClosed(building, id, true))
        {
            _audit.Push(building, "poll_close", id, "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(string id)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        if (_polls.SetClosed(building, id, false))
        {
            _audit.Push(building, "poll_reopen", id, "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var building = await _buildings.GetByIdAsync(BuildingId);
        if (building == null) return NotFound();
        if (_polls.Delete(building, id))
        {
            _audit.Push(building, "poll_delete", id, "admin", User.Identity?.Name ?? "admin");
            await _buildings.SaveFullAsync(building);
        }
        return RedirectToAction("Index");
    }
}
