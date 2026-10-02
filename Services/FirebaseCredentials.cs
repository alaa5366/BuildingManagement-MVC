namespace BuildingManagementMvc.Services;

// بيدور على بيانات الـ service account بالترتيب:
// 1) Firebase:ServiceAccountJson  (نص JSON كامل — مناسب لـ Azure environment variables)
// 2) ملف على القرص (Firebase:ServiceAccountJsonPath أو firebase-service-account.json)
public static class FirebaseCredentials
{
    public static string? TryLoadJson(IConfiguration config, string contentRoot)
    {
        var inline = config["Firebase:ServiceAccountJson"];
        if (!string.IsNullOrWhiteSpace(inline)) return inline;

        var path = config["Firebase:ServiceAccountJsonPath"];
        if (string.IsNullOrWhiteSpace(path)) path = "firebase-service-account.json";

        var full = Path.IsPathRooted(path) ? path : Path.Combine(contentRoot, path);
        return File.Exists(full) ? File.ReadAllText(full) : null;
    }
}
