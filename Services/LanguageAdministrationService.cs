using System.Globalization;
using System.Text.RegularExpressions;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public sealed record LanguageAdminRow(string CultureCode, string EnglishName, string NativeName, string Direction, string? FallbackCulture, string? Flag, bool IsEnabled, bool IsDefault, int SortOrder);
public sealed record TranslationAdminRow(string Key, string Category, string SourceGroup, string? Description, string Value, bool IsMissing, int LanguageCount);
public sealed record TranslationGroupRow(string Category, string SourceGroup, int Total, int Missing);
public sealed record LanguageAdminPage(IReadOnlyList<LanguageAdminRow> Languages, IReadOnlyList<TranslationGroupRow> Groups, IReadOnlyList<TranslationAdminRow> Entries, string? Culture, string? Category, string? Search, bool MissingOnly, int TotalKeys, int MissingCount);

public sealed class LanguageAdministrationService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditLogger _audit;
    private readonly ILogger<LanguageAdministrationService> _logger;
    private static readonly object CacheLock = new();
    private static Dictionary<string, Dictionary<string, string>> _runtime = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex CulturePattern = new(@"^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})*$", RegexOptions.Compiled);

    public LanguageAdministrationService(IDbContextFactory<AppDbContext> factory, IAuditLogger audit, ILogger<LanguageAdministrationService> logger)
    { _factory = factory; _audit = audit; _logger = logger; }

    public static string? GetRuntime(string key, string culture)
    {
        lock (CacheLock)
            return _runtime.TryGetValue(culture, out var values) && values.TryGetValue(key, out var value) ? value : null;
    }

    public async Task RefreshRuntimeAsync(CancellationToken ct = default)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            var languages = await db.Languages.AsNoTracking().Where(x => x.IsEnabled).Select(x => x.CultureCode).ToListAsync(ct);
            var values = await db.TranslationValues.AsNoTracking().Join(db.Languages.AsNoTracking().Where(l => l.IsEnabled), v => v.CultureCode, l => l.CultureCode, (v,l) => v).ToListAsync(ct);
            var cache = languages.ToDictionary(x => x, _ => new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
            foreach (var row in values) if (cache.TryGetValue(row.CultureCode, out var map)) map[row.Key] = row.Value;
            lock (CacheLock) _runtime = cache;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Dynamic localization cache refresh failed; resource files remain the fallback."); }
    }

    public async Task<LanguageAdminPage> GetPageAsync(string? culture, string? category, string? search, bool missingOnly, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var languages = await db.Languages.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.CultureCode)
            .Select(x => new LanguageAdminRow(x.CultureCode,x.EnglishName,x.NativeName,x.Direction,x.FallbackCulture,x.Flag,x.IsEnabled,x.IsDefault,x.SortOrder)).ToListAsync(ct);
        var selected = languages.FirstOrDefault(x => x.CultureCode == culture)?.CultureCode ?? languages.FirstOrDefault(x => x.IsDefault)?.CultureCode ?? "ar";
        var query = db.TranslationEntries.AsNoTracking().Where(x => !x.IsArchived);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Key.Contains(search) || x.SourceGroup.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        var entries = await query.OrderBy(x => x.Category).ThenBy(x => x.SourceGroup).ThenBy(x => x.Key).Take(1500).ToListAsync(ct);
        var values = await db.TranslationValues.AsNoTracking().Where(x => x.CultureCode == selected).ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        var rows = entries.Select(x => new TranslationAdminRow(x.Key,x.Category,x.SourceGroup,x.Description,values.GetValueOrDefault(x.Key) ?? "",!values.ContainsKey(x.Key),languages.Count(l => l.IsEnabled))).ToList();
        if (missingOnly) rows = rows.Where(x => x.IsMissing).ToList();
        var groups = rows.GroupBy(x => new { x.Category, x.SourceGroup }).Select(g => new TranslationGroupRow(g.Key.Category,g.Key.SourceGroup,g.Count(),g.Count(x=>x.IsMissing))).ToList();
        return new(languages,groups,rows,selected,category,search,missingOnly,entries.Count,rows.Count(x=>x.IsMissing));
    }

    public async Task<(bool Success,string Message)> SaveLanguageAsync(string culture,string englishName,string nativeName,string direction,string? fallback,string? flag,bool enabled,bool isDefault,int sortOrder,string actor,CancellationToken ct)
    {
        culture = (culture ?? "").Trim();
        if (!CulturePattern.IsMatch(culture) || !CultureInfo.GetCultures(CultureTypes.AllCultures).Any(x => x.Name.Equals(culture,StringComparison.OrdinalIgnoreCase)))
            return (false,"كود اللغة غير صحيح أو غير مدعوم في بيئة .NET.");
        if (string.IsNullOrWhiteSpace(englishName) || string.IsNullOrWhiteSpace(nativeName) || (direction!="RTL" && direction!="LTR"))
            return (false,"بيانات اللغة أو اتجاه الكتابة غير صحيحة.");
        if (!string.IsNullOrWhiteSpace(fallback) && (!CulturePattern.IsMatch(fallback) || fallback.Equals(culture,StringComparison.OrdinalIgnoreCase)))
            return (false,"لغة fallback غير صحيحة.");
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.Languages.FindAsync([culture],ct);
        if (entity == null) { entity = new LanguageEntity { CultureCode=culture }; db.Languages.Add(entity); }
        entity.EnglishName=englishName.Trim(); entity.NativeName=nativeName.Trim(); entity.Direction=direction;
        entity.FallbackCulture=string.IsNullOrWhiteSpace(fallback)?null:fallback.Trim(); entity.Flag=string.IsNullOrWhiteSpace(flag)?null:flag.Trim();
        entity.IsEnabled=enabled; entity.SortOrder=Math.Clamp(sortOrder,0,10000); entity.UpdatedAtUtc=DateTime.UtcNow; entity.UpdatedBy=actor;
        if (isDefault) { foreach (var item in await db.Languages.Where(x=>x.IsDefault).ToListAsync(ct)) item.IsDefault=false; entity.IsDefault=true; }
        else if (entity.IsDefault && !enabled) return (false,"لا يمكن تعطيل اللغة الافتراضية قبل اختيار لغة افتراضية بديلة.");
        if (!string.IsNullOrWhiteSpace(entity.FallbackCulture) && await WouldCreateFallbackCycleAsync(db,culture,entity.FallbackCulture,ct))
            return (false,"إعداد fallback يؤدي إلى حلقة ترجمة.");
        await db.SaveChangesAsync(ct);
        await _audit.LogAsync("Language.Configuration.Updated",userId:actor,userRole:"superadmin",metadata:new { culture, enabled, isDefault, direction });
        await RefreshRuntimeAsync(ct);
        return (true,"تم حفظ إعدادات اللغة.");
    }

    private static async Task<bool> WouldCreateFallbackCycleAsync(AppDbContext db,string culture,string fallback,CancellationToken ct)
    {
        var map=await db.Languages.AsNoTracking().ToDictionaryAsync(x=>x.CultureCode,x=>x.FallbackCulture, StringComparer.OrdinalIgnoreCase,ct);
        var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase){culture}; var current=fallback;
        while(!string.IsNullOrWhiteSpace(current)){ if(!seen.Add(current)) return true; current=map.GetValueOrDefault(current); }
        return false;
    }

    public async Task<(bool Success,string Message)> SaveTranslationAsync(string key,string category,string sourceGroup,string? description,string culture,string value,string actor,CancellationToken ct)
    {
        key=(key??"").Trim(); culture=(culture??"").Trim();
        if(string.IsNullOrWhiteSpace(key)||key.Length>200||string.IsNullOrWhiteSpace(value)||value.Length>4000) return (false,"المفتاح أو الترجمة غير صالحين.");
        await using var db=await _factory.CreateDbContextAsync(ct);
        var language=await db.Languages.FirstOrDefaultAsync(x=>x.CultureCode==culture&&x.IsEnabled,ct);
        if(language==null) return (false,"اللغة غير موجودة أو غير مفعلة.");
        var entry=await db.TranslationEntries.FindAsync([key],ct);
        if(entry==null){entry=new TranslationEntryEntity{Key=key};db.TranslationEntries.Add(entry);}
        entry.Category=NormalizeCategory(category);entry.SourceGroup=string.IsNullOrWhiteSpace(sourceGroup)?"Shared":sourceGroup.Trim()[..Math.Min(160,sourceGroup.Trim().Length)];
        entry.Description=string.IsNullOrWhiteSpace(description)?null:description.Trim()[..Math.Min(500,description.Trim().Length)];
        entry.UpdatedAtUtc=DateTime.UtcNow;entry.UpdatedBy=actor;entry.IsArchived=false;
        var translation=await db.TranslationValues.FindAsync([key,culture],ct);
        if(translation==null){translation=new TranslationValueEntity{Key=key,CultureCode=culture};db.TranslationValues.Add(translation);}
        translation.Value=value.Trim();translation.UpdatedAtUtc=DateTime.UtcNow;translation.UpdatedBy=actor;
        await db.SaveChangesAsync(ct);
        await _audit.LogAsync("Language.Translation.Updated",userId:actor,userRole:"superadmin",metadata:new { key,culture,category=entry.Category,sourceGroup=entry.SourceGroup });
        await RefreshRuntimeAsync(ct);
        return (true,"تم حفظ الترجمة.");
    }

    public async Task<int> ScanSourceFilesAsync(string contentRoot, string actor, CancellationToken ct)
    {
        var root = Path.GetFullPath(contentRoot);
        var folders = new[] { ("Controllers","Controllers"),("Services","Services"),("Views","Views"),(Path.Combine("wwwroot","js"),"JavaScript") };
        var discovered = new Dictionary<string,(string Category,string Group)>(StringComparer.OrdinalIgnoreCase);
        var keyPattern = new Regex(@"(?:Loc\\.T|Loc\\.Js|Loc\\.JsonDict|LocalizationService\\.Get)\\s*\\(\\s*[\""]([A-Za-z0-9_.-]+)[\""]", RegexOptions.Compiled);
        foreach (var (folder, category) in folders)
        {
            var path = Path.Combine(root, folder);
            if (!Directory.Exists(path)) continue;
            foreach (var file in Directory.EnumerateFiles(path, category=="Views"?"*.cshtml":category=="JavaScript"?"*.js":"*.cs", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var group = Path.GetRelativePath(path,file).Replace('\\','/');
                if (group.Length > 160) group = group[^160..];
                string source;
                try { source = await File.ReadAllTextAsync(file,ct); } catch (IOException) { continue; }
                foreach (Match match in keyPattern.Matches(source))
                    discovered.TryAdd(match.Groups[1].Value,(category,group));
            }
        }

        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.TranslationEntries.ToDictionaryAsync(x=>x.Key,StringComparer.OrdinalIgnoreCase,ct);
        foreach (var item in discovered)
        {
            if (existing.TryGetValue(item.Key,out var row))
            {
                row.Category=item.Value.Category; row.SourceGroup=item.Value.Group;
            }
            else db.TranslationEntries.Add(new TranslationEntryEntity { Key=item.Key, Category=item.Value.Category, SourceGroup=item.Value.Group, UpdatedAtUtc=DateTime.UtcNow, UpdatedBy=actor });
        }

        var resources = Path.Combine(root,"Resources");
        if (Directory.Exists(resources))
        {
            foreach (var file in Directory.EnumerateFiles(resources,"Shared*.resx",SearchOption.TopDirectoryOnly))
            {
                var name=Path.GetFileNameWithoutExtension(file);
                var culture=name=="Shared"?"ar":name.StartsWith("Shared.",StringComparison.OrdinalIgnoreCase)?name["Shared.".Length..]:"ar";
                if (!await db.Languages.AnyAsync(x=>x.CultureCode==culture&&x.IsEnabled,ct)) continue;
                var document=System.Xml.Linq.XDocument.Load(file);
                foreach (var data in document.Root?.Elements("data") ?? [])
                {
                    var key=(string?)data.Attribute("name"); var value=(string?)data.Element("value");
                    if (string.IsNullOrWhiteSpace(key)||string.IsNullOrWhiteSpace(value)||key.Length>200||value.Length>4000) continue;
                    if (!existing.ContainsKey(key)) { var entry=new TranslationEntryEntity{Key=key,Category="Shared",SourceGroup="Resources/Shared.resx",UpdatedAtUtc=DateTime.UtcNow,UpdatedBy=actor}; db.TranslationEntries.Add(entry); existing[key]=entry; }
                    var translation=await db.TranslationValues.FindAsync([key,culture],ct);
                    if (translation==null) db.TranslationValues.Add(new TranslationValueEntity{Key=key,CultureCode=culture,Value=value,UpdatedAtUtc=DateTime.UtcNow,UpdatedBy=actor});
                }
            }
        }
        await db.SaveChangesAsync(ct);
        await _audit.LogAsync("Language.SourceScan.Completed",userId:actor,userRole:"superadmin",metadata:new { discovered=discovered.Count, root });
        await RefreshRuntimeAsync(ct);
        return discovered.Count;
    }

    public async Task<string> ExportCsvAsync(CancellationToken ct)
    {
        await using var db=await _factory.CreateDbContextAsync(ct);
        var rows=await (from e in db.TranslationEntries.AsNoTracking().Where(x=>!x.IsArchived)
                        join v in db.TranslationValues.AsNoTracking() on e.Key equals v.Key
                        orderby e.Category,e.SourceGroup,e.Key,v.CultureCode
                        select new { e.Key,e.Category,e.SourceGroup,e.Description,v.CultureCode,v.Value }).ToListAsync(ct);
        static string Q(string? value) => "\""+(value??"").Replace("\"","\"\"")+"\"";
        var lines=new List<string>{"Key,Category,SourceGroup,Description,Culture,Value"};
        lines.AddRange(rows.Select(x=>string.Join(",",Q(x.Key),Q(x.Category),Q(x.SourceGroup),Q(x.Description),Q(x.CultureCode),Q(x.Value))));
        return "\uFEFF"+string.Join("\r\n",lines);
    }

    private static string NormalizeCategory(string? value) => value is "Controllers" or "Services" or "Views" or "Shared" or "JavaScript" or "Validation" or "System" ? value : "System";
}
