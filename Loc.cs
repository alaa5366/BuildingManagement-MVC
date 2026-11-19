using System.Globalization;
using System.Resources;
using System.Text.Json;
using System.Web;

namespace BuildingManagementMvc;

// ✅ نقطة الترجمة الوحيدة في التطبيق.
// - T("Key") / T("Key", args) بترجع النص بلغة الـ request الحالية (ar / en) من Resources/Shared.*.resx
// - لو المفتاح مش موجود بترجع المفتاح نفسه (بيبان في الشاشة بدل ما يطلع فاضي)
// - static عشان تشتغل في الـ Views والـ Controllers والـ Services (وحتى الـ static methods) من غير حقن
/// <summary>
/// Static localization helper. <c>T</c> returns a translated string for the current UI culture and <c>Js</c> returns one that is safe inside a JavaScript string.
/// </summary>
public static class Loc
{
    private static readonly ResourceManager Rm =
        new("BuildingManagementMvc.Resources.Shared", typeof(Loc).Assembly);

    /// <summary>
    /// Returns the t.
    /// </summary>
    public static string T(string key)
    {
        try
        {
            var dynamicValue = BuildingManagementMvc.Services.LanguageAdministrationService.GetRuntime(key, CultureInfo.CurrentUICulture.Name);
            if (!string.IsNullOrWhiteSpace(dynamicValue)) return dynamicValue;
            return Rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }
        catch (MissingManifestResourceException)
        {
            return key;
        }
    }

    /// <summary>
    /// Returns the t.
    /// </summary>
    public static string T(string key, params object?[] args)
    {
        var format = T(key);
        if (args == null || args.Length == 0) return format;
        try
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }
        catch (FormatException)
        {
            return format;
        }
    }

    // للاستخدام جوه JavaScript (داخل سكريبت أو onclick): بيعمل escape للنص
    /// <summary>
    /// Returns the js.
    /// </summary>
    public static string Js(string key, params object?[] args) =>
        HttpUtility.JavaScriptStringEncode(T(key, args));

    // للـ JavaScript الثابت (wwwroot/js): بيطلّع JSON بالمفاتيح المطلوبة بلغة الـ request
    // الاستخدام في الـ View: <script>window.I18N = Object.assign(window.I18N || {}, @Html.Raw(Loc.JsonDict("Js_Close")));</script>
    /// <summary>
    /// Returns the JSON dict.
    /// </summary>
    public static string JsonDict(params string[] keys)
    {
        var d = new Dictionary<string, string>();
        foreach (var k in keys) d[k] = T(k);
        return JsonSerializer.Serialize(d);
    }
}
