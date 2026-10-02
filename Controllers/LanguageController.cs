using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[AllowAnonymous]
public class LanguageController : Controller
{
    // ✅ اللغات المدعومة
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "en", "fr", "de"
    };

    [HttpGet]
    public IActionResult Set(string lang, string? returnUrl)
    {
        // ✅ لو اللغة مش مدعومة، ارجع للعربي
        var value = SupportedLanguages.Contains(lang) ? lang.ToLowerInvariant() : "ar";

        // ✅ نستخدم صيغة ASP.NET Core القياسية
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(
            new RequestCulture(value));

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,  // ".AspNetCore.Culture"
            cookieValue,                                     // "c=ar|uic=ar"
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

        // ✅ نحدّث الكوكي بتاعنا القديم كمان (للتوافق)
        Response.Cookies.Append("bm_lang", value, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = false,
            SameSite = SameSiteMode.Lax
        });

        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}