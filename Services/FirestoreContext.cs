using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Services;

// يفتح اتصال واحد بقاعدة بيانات Firestore ويُستخدم كـ Singleton
public class FirestoreContext
{
    public FirestoreDb Db { get; }

    public FirestoreContext(IConfiguration config)
    {
        var projectId = config["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Firebase:ProjectId غير موجود في appsettings.json");

        var credPath = config["Firebase:ServiceAccountJsonPath"];

        var builder = new FirestoreDbBuilder { ProjectId = projectId };

        // لو حطيت ملف حساب الخدمة (Service Account JSON) بيتقرأ منه،
        // غير كده هيحاول ياخد الاعتماد من متغير البيئة
        // GOOGLE_APPLICATION_CREDENTIALS (Application Default Credentials).
        if (!string.IsNullOrWhiteSpace(credPath) && File.Exists(credPath))
        {
            builder.JsonCredentials = File.ReadAllText(credPath);
        }

        Db = builder.Build();
    }
}
