using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Services;

// يفتح اتصال واحد بقاعدة بيانات Firestore ويُستخدم كـ Singleton
public class FirestoreContext
{
    public FirestoreDb Db { get; }

    public FirestoreContext(IConfiguration config, IWebHostEnvironment env)
    {
        var projectId = config["Firebase:ProjectId"]
            ?? throw new InvalidOperationException("Firebase:ProjectId غير موجود في appsettings.json");

        var builder = new FirestoreDbBuilder { ProjectId = projectId };

        // الأولوية: Firebase:ServiceAccountJson (متغير بيئة) ← ملف على القرص ← Application Default Credentials
        var json = FirebaseCredentials.TryLoadJson(config, env.ContentRootPath);
        if (json != null)
        {
            builder.JsonCredentials = json;
        }

        Db = builder.Build();
    }
}
