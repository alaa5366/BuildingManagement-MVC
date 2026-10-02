namespace BuildingManagementMvc.Services;

// قراءة الأسرار من الـ configuration بس (appsettings / متغيرات البيئة / user-secrets).
// لو السر مش موجود أو لسه "CHANGE_ME" التطبيق يفشل بوضوح بدل ما يشتغل بمفتاح معروف.
public static class ConfigSecrets
{
    public static string Require(IConfiguration config, string key, params string[] fallbackKeys)
    {
        foreach (var k in new[] { key }.Concat(fallbackKeys))
        {
            var v = config[k];
            if (!string.IsNullOrWhiteSpace(v) &&
                !v.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
                return v;
        }

        throw new InvalidOperationException(
            $"Missing required configuration '{key}'. Set it in appsettings.Development.json / user-secrets locally, " +
            $"or as an environment variable named '{key.Replace(":", "__")}' (e.g. Azure App Service > Environment variables).");
    }
}
