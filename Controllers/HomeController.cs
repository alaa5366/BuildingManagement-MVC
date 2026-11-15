using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  HomeController	الصفحة الرئيسية (Redirect → LoginChoice)
// ============================================================
/// <summary>
/// Public landing endpoints: home redirect, privacy page and the error page.
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Shows the main page of the home screen.
    /// </summary>
    public IActionResult Index()
    {
        return RedirectToAction("LoginChoice", "Account");
    }

    /// <summary>
    /// Shows the privacy page.
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Shows the error page.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exception = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Error;
        if (exception is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            ViewBag.ErrorMessage = "تم تعديل بيانات العمارة بواسطة مستخدم آخر. أعد تحميل الصفحة ثم حاول مرة أخرى.";
        }
        return View();
    }
}
