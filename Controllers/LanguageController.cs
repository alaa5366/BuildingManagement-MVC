using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

// نفس فكرة applyLanguage/savedLang في js/i18n/language.js — هنا بكوكي
// بسيط بدل localStorage
[AllowAnonymous]
public class LanguageController : Controller
{
    [HttpGet]
    public IActionResult Set(string lang, string? returnUrl)
    {
        var value = lang == "en" ? "en" : "ar";
        Response.Cookies.Append("bm_lang", value, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true
        });
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
