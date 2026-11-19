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
    private readonly IWebHostEnvironment _environment;
    public LanguageManagementController(LanguageAdministrationService service, IWebHostEnvironment environment) { _service=service; _environment=environment; }

    /// <summary>Displays the administration page and its current records.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? culture,string? category,string? search,bool missingOnly=false,CancellationToken ct=default)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        await _service.RefreshRuntimeAsync(ct);
        return View(await _service.GetPageAsync(culture,category,search,missingOnly,ct));
    }

    /// <summary>Scans application source files for localization resources.</summary>
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanSources(CancellationToken ct)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        var actor=User.FindFirstValue(ClaimTypes.NameIdentifier); if(string.IsNullOrWhiteSpace(actor)) return Forbid();
        var count=await _service.ScanSourceFilesAsync(_environment.ContentRootPath,actor,ct);
        TempData["Message"]="تم فحص ملفات المصدر واستيراد الموارد الحالية. عدد المفاتيح المكتشفة: "+count+".";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Exports localization data as a CSV file.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv(CancellationToken ct)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        var csv=await _service.ExportCsvAsync(ct);
        return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(),"text/csv; charset=utf-8","translations.csv");
    }

    /// <summary>Imports localization data from a CSV file.</summary>
    [HttpPost,ValidateAntiForgeryToken,RequestSizeLimit(5*1024*1024)]
    public async Task<IActionResult> ImportCsv(IFormFile? file,CancellationToken ct)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        var actor=User.FindFirstValue(ClaimTypes.NameIdentifier); if(string.IsNullOrWhiteSpace(actor)) return Forbid();
        if(file==null||file.Length==0||file.Length>5*1024*1024){TempData["Error"]="ملف CSV غير صالح أو أكبر من 5 ميجابايت.";return RedirectToAction(nameof(Index));}
        using var reader=new StreamReader(file.OpenReadStream(),System.Text.Encoding.UTF8,true);
        var rows=new List<TranslationImportRow>(); var lineNumber=0;
        while(await reader.ReadLineAsync(ct) is string line){lineNumber++;if(lineNumber==1||string.IsNullOrWhiteSpace(line))continue;
            var fields=ParseCsvLine(line);if(fields.Count!=6)continue;
            rows.Add(new TranslationImportRow(fields[0],fields[1],fields[2],fields[3],fields[4],fields[5]));
            if(rows.Count>=5000) break;
        }
        var count=await _service.ImportTranslationsAsync(rows,actor,ct);
        TempData["Message"]="تم استيراد أو تحديث "+count+" ترجمة."; return RedirectToAction(nameof(Index));
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result=new List<string>();var value=new System.Text.StringBuilder();var quoted=false;
        for(var i=0;i<line.Length;i++){var ch=line[i];if(ch=='\"'){if(quoted&&i+1<line.Length&&line[i+1]=='\"'){value.Append('\"');i++;}else quoted=!quoted;}else if(ch==','&&!quoted){result.Add(value.ToString());value.Clear();}else value.Append(ch);}
        result.Add(value.ToString());return result;
    }

    /// <summary>Creates or updates a language definition.</summary>
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLanguage(string culture,string englishName,string nativeName,string direction,string? fallback,string? flag,bool enabled,bool isDefault,int sortOrder,CancellationToken ct)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        var actor=User.FindFirstValue(ClaimTypes.NameIdentifier); if(string.IsNullOrWhiteSpace(actor)) return Forbid();
        var result=await _service.SaveLanguageAsync(culture,englishName,nativeName,direction,fallback,flag,enabled,isDefault,sortOrder,actor,ct);
        TempData[result.Success?"Message":"Error"]=result.Message; return RedirectToAction(nameof(Index),new{culture});
    }

    /// <summary>Creates or updates a translation entry.</summary>
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTranslation(string key,string category,string sourceGroup,string? description,string culture,string value,CancellationToken ct)
    {
        if(User.HasClaim("impersonated","true")) return Forbid();
        var actor=User.FindFirstValue(ClaimTypes.NameIdentifier); if(string.IsNullOrWhiteSpace(actor)) return Forbid();
        var result=await _service.SaveTranslationAsync(key,category,sourceGroup,description,culture,value,actor,ct);
        TempData[result.Success?"Message":"Error"]=result.Message; return RedirectToAction(nameof(Index),new{culture,category,search=key});
    }
}
