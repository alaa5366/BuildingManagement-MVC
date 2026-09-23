using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction("LoginChoice", "Account");

        if (User.IsInRole("superadmin")) return RedirectToAction("Index", "SuperAdminHome");
        if (User.IsInRole("admin")) return RedirectToAction("Index", "AdminHome");
        if (User.IsInRole("resident")) return RedirectToAction("Index", "ResidentHome");

        return RedirectToAction("LoginChoice", "Account");
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
