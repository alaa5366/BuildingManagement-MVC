namespace BuildingManagementMvc.Services;

// Background Service — بيفحص كل 30 دقيقة
public class BackupScheduler : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BackupScheduler> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public BackupScheduler(IServiceProvider services, ILogger<BackupScheduler> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[BackupScheduler] Started. Check interval: {Interval}", _checkInterval);

        // ✅ نستنى 30 ثانية أول مرة
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ScheduledBackupService>();
                var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

                if (await service.ShouldRunAsync())
                {
                    _logger.LogInformation("[BackupScheduler] Time to run!");
                    await service.RunScheduledBackupAsync(env);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BackupScheduler] Error in check cycle");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("[BackupScheduler] Stopped");
    }
}