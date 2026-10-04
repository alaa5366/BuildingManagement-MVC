// =====================================================================
//  HealthMonitorService - بيفحص Firebase كل 30 ثانية
//
//  السلوك:
//  - Firebase وقع → SetModeAsync("Sql") تلقائيًا
//  - Firebase رجع → ReverseSync ثم SetModeAsync("Dual")
// =====================================================================
using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Services;

public class HealthMonitorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HealthMonitorService> _log;
    private readonly FirestoreDb _firestore;

    private bool _firebaseWasDown = false;
    private const int CheckIntervalSeconds = 30;

    public HealthMonitorService(
        IServiceProvider services,
        ILogger<HealthMonitorService> log,
        FirestoreContext firestoreCtx)
    {
        _services = services;
        _log = log;
        _firestore = firestoreCtx.Db;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // انتظر 10 ثواني عند بدء التطبيق
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var storageSettings = scope.ServiceProvider.GetRequiredService<StorageSettingsService>();
                var reverseSync = scope.ServiceProvider.GetService<ReverseSyncService>();

                var isHealthy = await CheckFirebaseHealthAsync();
                var currentMode = await storageSettings.GetModeAsync();

                if (!isHealthy && !_firebaseWasDown && currentMode != "Sql")
                {
                    _log.LogError("🔥 Firebase is DOWN! Switching to SQL mode...");
                    await storageSettings.SetModeAsync("Sql", "health-monitor");
                    _firebaseWasDown = true;
                }
                else if (isHealthy && _firebaseWasDown)
                {
                    _log.LogInformation("✅ Firebase is BACK! Starting reverse sync...");

                    if (reverseSync != null)
                    {
                        try
                        {
                            var report = await reverseSync.SyncSqlToFirebaseAsync();
                            _log.LogInformation(
                                "Reverse sync: {Added} added, {Updated} updated, {Errors} errors",
                                report.Added, report.Updated, report.Errors.Count);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "Reverse sync failed");
                        }
                    }

                    await storageSettings.SetModeAsync("Dual", "health-monitor");
                    _firebaseWasDown = false;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Health check iteration failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), stoppingToken);
        }
    }

    private async Task<bool> CheckFirebaseHealthAsync()
    {
        try
        {
            // محاولة قراءة بسيطة
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var doc = await _firestore
                .Collection("_health")
                .Document("ping")
                .GetSnapshotAsync(cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

// =====================================================================
//  ReverseSyncReport - نتيجة المزامنة العكسية
// =====================================================================
public class ReverseSyncReport
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Deleted { get; set; }
    public TimeSpan Elapsed { get; set; }
    public List<string> Errors { get; set; } = new();
}