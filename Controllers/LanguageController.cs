using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  LanguageController	   تغيير اللغة (ar/en/fr/de)
// ============================================================
[AllowAnonymous]
public class LanguageController : Controller
{
    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "en", "fr", "de"
    };

    [HttpGet]
    public IActionResult Set(string lang, string? returnUrl)
    {
        var value = SupportedLanguages.Contains(lang) ? lang.ToLowerInvariant() : "ar";

        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(
            new RequestCulture(value));

        // ✅ إعدادات الكوكي بشكل صريح ومضمون
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cookieValue,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = false, // عشان نقدر نقرأها من الـ JS لو احتجنا
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps // في localhost بيكون false، فبيشتغل عادي
            });

        // ✅ كوكي إضافية للاحتياط
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