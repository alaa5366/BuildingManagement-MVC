using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("LoginChoice", "Account");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
