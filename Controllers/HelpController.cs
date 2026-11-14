using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  HelpController	دليل المستخدم
// ============================================================
[AllowAnonymous]
public class HelpController : Controller
{
    public IActionResult Index() => View();
}