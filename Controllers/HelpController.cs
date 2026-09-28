using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[AllowAnonymous]
public class HelpController : Controller
{
    public IActionResult Index() => View();
}