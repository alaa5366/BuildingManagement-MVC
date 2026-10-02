using System.Globalization;
using System.Resources;
using System.Text.Json;
using System.Web;

namespace BuildingManagementMvc;

// ✅ نقطة الترجمة الوحيدة في التطبيق.
// - T("Key") / T("Key", args) بترجع النص بلغة الـ request الحالية (ar / en) من Resources/Shared.*.resx
// - لو المفتاح مش موجود بترجع المفتاح نفسه (بيبان في الشاشة بدل ما يطلع فاضي)
// - static عشان تشتغل في الـ Views والـ Controllers والـ Services (وحتى الـ static methods) من غير حقن
public static class Loc
{
    private static readonly ResourceManager Rm =
        new("BuildingManagementMvc.Resources.Shared", typeof(Loc).Assembly);

    public static string T(string key)
    {
        try
        {
            return Rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }
        catch (MissingManifestResourceException)
        {
            return key;
        }
    }

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
    public static string Js(string key, params object?[] args) =>
        HttpUtility.JavaScriptStringEncode(T(key, args));

    // للـ JavaScript الثابت (wwwroot/js): بيطلّع JSON بالمفاتيح المطلوبة بلغة الـ request
    // الاستخدام في الـ View: <script>window.I18N = Object.assign(window.I18N || {}, @Html.Raw(Loc.JsonDict("Js_Close")));</script>
    public static string JsonDict(params string[] keys)
    {
        var d = new Dictionary<string, string>();
        foreach (var k in keys) d[k] = T(k);
        return JsonSerializer.Serialize(d);
    }
}
