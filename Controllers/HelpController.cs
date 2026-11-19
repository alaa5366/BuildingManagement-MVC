using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  HelpController	دليل المستخدم
// ============================================================
/// <summary>
/// Serves the in-app user guide.
/// </summary>
/// <remarks>
/// Access: anonymous.
/// </remarks>
[AllowAnonymous]
public class HelpController : Controller
{
    /// <summary>
    /// Shows the main page of the help screen.
    /// </summary>
    public IActionResult Index() => View();
}
