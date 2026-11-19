﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Controllers;
// ============================================================
//  LanguageController	   تغيير اللغة (ar/en/fr/de)
// ============================================================
/// <summary>
/// Changes the UI language by writing the culture cookie.
/// </summary>
/// <remarks>
/// Access: anonymous.
/// </remarks>
[AllowAnonymous]
public class LanguageController : Controller
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public LanguageController(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>
    /// Sets the language data.
    /// </summary>
    /// <remarks>
    /// HTTP GET.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Set(string lang, string? returnUrl, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var value = await db.Languages.AsNoTracking().AnyAsync(x => x.CultureCode == lang && x.IsEnabled, ct) ? lang.ToLowerInvariant() : "ar";

        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(
            new RequestCulture(value));

        // âœ… إعدادات الكوكي بشكل صريح ومضمون
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

        // âœ… كوكي إضافية للاحتياط
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
