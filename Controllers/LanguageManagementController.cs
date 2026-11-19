using System.Security.Claims;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagementMvc.Controllers;

[Authorize(Roles="superadmin")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class LanguageManagementController : Controller
{
    private readonly LanguageAdministrationService _service;
    public LanguageManagementController(LanguageAdministrationService service) => _service=service;

    [HttpGet]
    public async Task<IActionResult> Index(string? culture,string? category,string? search,bool missingOnly=false,CancellationToken ct=default)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        return View(await _service.GetPageAsync(culture,category,search,missingOnly,ct));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanSources(CancellationToken ct)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        var actor=User.FindFirstValue(ClaimTypes.NameIdentifier); if(string.IsNullOrWhiteSpace(actor)) return Forbid();
        var count=await _service.ScanSourceFilesAsync(Directory.GetCurrentDirectory(),actor,ct);
        TempData["Message"]="تم فحص ملفات المصدر واستيراد الموارد الحالية. عدد المفاتيح المكتشفة: "+count+".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLanguage(string culture,string englishName,string nativeName,string direction,string? fallback,string? flag,bool enabled,bool isDefault,int sortOrder,CancellationToken ct)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        var actor=User.FindFirstValue(ClaimTypes.NameIdentifier); if(string.IsNullOrWhiteSpace(actor)) return Forbid();
        var result=await _service.SaveLanguageAsync(culture,englishName,nativeName,direction,fallback,flag,enabled,isDefault,sortOrder,actor,ct);
        TempData[result.Success?"Message":"Error"]=result.Message; return RedirectToAction(nameof(Index),new{culture});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTranslation(string key,string category,string sourceGroup,string? description,string culture,string value,CancellationToken ct)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        var actor=User.FindFirstValue(ClaimTypes.NameIdentifier); if(string.IsNullOrWhiteSpace(actor)) return Forbid();
        var result=await _service.SaveTranslationAsync(key,category,sourceGroup,description,culture,value,actor,ct);
        TempData[result.Success?"Message":"Error"]=result.Message; return RedirectToAction(nameof(Index),new{culture,category,search=key});
    }
}
