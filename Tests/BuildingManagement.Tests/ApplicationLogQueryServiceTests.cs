using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ApplicationLogQueryServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bm-log-tests-" + Guid.NewGuid().ToString("N"));

    public ApplicationLogQueryServiceTests() =>
        Directory.CreateDirectory(Path.Combine(_root, "logs", "serilog"));

    [Fact]
    public async Task QueryPageAsync_FiltersAndReturnsNewestMatchingEvent()
    {
        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [INF] First event {\"SourceContext\":\"Tests.First\",\"CorrelationId\":\"corr-1\"}",
            "2026-11-17 10:01:00.000 +00:00 [WRN] Second event {\"SourceContext\":\"Tests.Second\",\"CorrelationId\":\"corr-2\"}",
            "2026-11-17 10:02:00.000 +00:00 [ERR] Third event {\"SourceContext\":\"Tests.Third\",\"CorrelationId\":\"corr-3\"}");

        var result = await CreateService().QueryPageAsync(new ApplicationLogQuery { Level = "Warning" });

        var item = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Second event", item.Message);
        Assert.Equal("corr-2", item.CorrelationId);
        Assert.Equal("Tests.Second", item.SourceContext);
    }

    [Fact]
    public async Task QueryPageAsync_RedactsSecretsAndCollectsExceptionContinuation()
    {
        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [ERR] Login failed password=super-secret user=alaa@example.com apiKey=api-secret Server=prod-db;Password=db-secret client payload ip=192.168.10.20 {\"SourceContext\":\"Tests.Auth\"}",
            "System.InvalidOperationException: eyJabcdefghijk.eyJabcdefghijk.eyJabcdefghijk",
            "   at Tests.Auth.Run()");

        var item = Assert.Single((await CreateService().QueryPageAsync(new ApplicationLogQuery())).Items);
        Assert.True(item.HasException);
        Assert.DoesNotContain("super-secret", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("alaa@example.com", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("api-secret", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("prod-db", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("db-secret", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("192.168.10.20", item.Message, StringComparison.Ordinal);
        Assert.Contains("[REDACTED_IP]", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJabcdefghijk", item.Exception, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", item.Message, StringComparison.Ordinal);
        Assert.Contains("[REDACTED_TOKEN]", item.Exception, StringComparison.Ordinal);
        Assert.Contains("Tests.Auth.Run", item.Exception, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueryPageAsync_RedactsWindowsAndUnixPathsFromAllDisplayedFields()
    {
        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [ERR] Physical path: 'D:\\Projects\\Building Management\\wwwroot\\logs\\app.txt' {}",
            "System.IO.IOException: Failed to open /var/log/building management/app.log",
            "2026-11-17 10:01:00.000 +00:00 [INF] Resolved file: /opt/building management/data/file.db",
            "Additional detail: /home/alaa/building data/config.json {\"SourceContext\":\"/srv/building management/Logger\"}");

        var result = await CreateService().QueryPageAsync(new ApplicationLogQuery());

        Assert.Equal(2, result.Items.Count);
        var error = result.Items.Single(x => x.Level == "Error");
        var information = result.Items.Single(x => x.Level == "Information");

        Assert.Contains("[REDACTED_PATH]", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(@"D:\Projects\Building Management", error.Message, StringComparison.Ordinal);
        Assert.Contains("[REDACTED_PATH]", error.Exception, StringComparison.Ordinal);
        Assert.DoesNotContain("/var/log/building management", error.Exception, StringComparison.Ordinal);

        Assert.Contains("[REDACTED_PATH]", information.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("/opt/building management", information.Message, StringComparison.Ordinal);
        Assert.Contains("[REDACTED_PATH]", information.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("/home/alaa/building data", information.Details, StringComparison.Ordinal);
        Assert.Equal("[REDACTED_PATH]", information.SourceContext);
    }

    [Fact]
    public async Task QueryPageAsync_AppliesCorrelationAndExceptionFilters()
    {
        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [INF] Healthy {\"CorrelationId\":\"target-id\"}",
            "2026-11-17 10:01:00.000 +00:00 [ERR] Failed {\"CorrelationId\":\"other-id\"}",
            "System.Exception: failure");

        var result = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            CorrelationId = "target", HasException = false
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("target-id", item.CorrelationId);
        Assert.False(item.HasException);
    }

    [Fact]
    public async Task QueryPageAsync_CapturesOrdinaryContinuationAsDetailsWithoutTreatingItAsException()
    {
        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [INF] Executed DbCommand (1ms) {}",
            "CommandType='Text', CommandTimeout='30'");

        var item = Assert.Single((await CreateService().QueryPageAsync(new ApplicationLogQuery())).Items);

        Assert.False(item.HasException);
        Assert.Empty(item.Exception);
        Assert.Empty(item.CorrelationId);
        Assert.Contains("CommandType='Text'", item.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueryPageAsync_PreservesAssemblyVersionsAndRedactsValidIpAddresses()
    {
        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [INF] Runtime Version=8.0.34.0 ClientIp=192.168.10.20 {}");

        var item = Assert.Single((await CreateService().QueryPageAsync(new ApplicationLogQuery())).Items);

        Assert.Contains("Version=8.0.34.0", item.Message, StringComparison.Ordinal);
        Assert.Contains("[REDACTED_IP]", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("192.168.10.20", item.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueryPageAsync_ExtractsPropertiesFromMultilineEventContinuation()
    {
        const string correlationId = "7c60f16e7bee4eafba695a642f268807";
        const string sourceContext = "Microsoft.EntityFrameworkCore.Database.Command";

        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [INF] Executed DbCommand (4ms) [Parameters=[], CommandType='\"Text\"', CommandTimeout='30']",
            "SET IMPLICIT_TRANSACTIONS OFF;",
            "UPDATE [Presence] SET [LastSeen] = @p0",
            "WHERE [Uid] = @p1; {\"EventId\":{\"Id\":20101},\"SourceContext\":\"" + sourceContext +
            "\",\"CorrelationId\":\"" + correlationId + "\"}");

        var result = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            CorrelationId = correlationId
        });

        var item = Assert.Single(result.Items);

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Executed DbCommand (4ms) [Parameters=[], CommandType='\"Text\"', CommandTimeout='30']", item.Message);
        Assert.Equal(correlationId, item.CorrelationId);
        Assert.Equal(sourceContext, item.SourceContext);
        Assert.Contains("SET IMPLICIT_TRANSACTIONS OFF;", item.Details, StringComparison.Ordinal);
        Assert.Contains("UPDATE [Presence]", item.Details, StringComparison.Ordinal);
        Assert.Contains("WHERE [Uid] = @p1;", item.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("CorrelationId", item.Details, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceContext", item.Details, StringComparison.Ordinal);
        Assert.False(item.HasException);
        Assert.Empty(item.Exception);

        var searched = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            Search = "UPDATE [Presence]"
        });
        Assert.Single(searched.Items);
    }

    [Fact]
    public async Task QueryPageAsync_ClampsPagingAndIgnoresFilesOutsideLogDirectory()
    {
        WriteLog("2026-11-17 10:00:00.000 +00:00 [INF] Safe event {}");
        File.WriteAllText(Path.Combine(_root, "outside.txt"),
            "2026-11-17 10:01:00.000 +00:00 [ERR] Outside event {}");

        var result = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            Page = int.MaxValue, PageSize = int.MaxValue
        });

        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(100, result.PageSize);
        Assert.Equal(1, result.TotalItems);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task QueryPageAsync_FiltersByComponentTypeUsingExplicitSourceContext()
    {
        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [ERR] Controller failure {\"SourceContext\":\"BuildingManagementMvc.Controllers.BuildingsController\"}",
            "2026-11-17 10:01:00.000 +00:00 [ERR] Service failure {\"SourceContext\":\"BuildingManagementMvc.Services.PresenceService\"}",
            "2026-11-17 10:02:00.000 +00:00 [ERR] Database failure {\"SourceContext\":\"Microsoft.EntityFrameworkCore.Database.Command\"}",
            "2026-11-17 10:03:00.000 +00:00 [ERR] Unknown source {}");

        var controllers = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            ComponentType = "Controller"
        });
        var services = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            ComponentType = "Service"
        });
        var databases = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            ComponentType = "Database"
        });
        var other = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            ComponentType = "Other"
        });

        var controller = Assert.Single(controllers.Items);
        Assert.Equal("BuildingManagementMvc.Controllers.BuildingsController", controller.SourceContext);
        Assert.Equal("BuildingsController", controller.ComponentName);
        Assert.Equal("Controller", controller.ComponentType);
        Assert.Equal("PresenceService", Assert.Single(services.Items).ComponentName);
        Assert.Equal("Microsoft.EntityFrameworkCore.Database.Command", Assert.Single(databases.Items).SourceContext);
        Assert.Equal("Unknown source", Assert.Single(other.Items).Message);
    }

    [Fact]
    public async Task QueryPageAsync_ExtractsHttpStatusAndResponseLatency()
    {
        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [INF] HTTP GET /AuditLog responded 200 in 12.3456 ms {\"SourceContext\":\"Serilog.AspNetCore.RequestLoggingMiddleware\",\"CorrelationId\":\"request-42\"}");

        var item = Assert.Single((await CreateService().QueryPageAsync(new ApplicationLogQuery())).Items);

        Assert.Equal("GET", item.HttpMethod);
        Assert.Equal("/AuditLog", item.RequestPath);
        Assert.Equal(200, item.HttpStatusCode);
        Assert.Equal(12.3456, item.DurationMs);
        Assert.Equal("request-42", item.CorrelationId);
    }


    [Fact]
    public async Task QueryPageAsync_FiltersHttpEventsByStatusAndLatency()
    {
        WriteLog(
            "2026-11-17 10:00:00.000 +00:00 [INF] HTTP GET /fast responded 200 in 8.5 ms {}",
            "2026-11-17 10:00:01.000 +00:00 [WRN] HTTP GET /slow responded 200 in 850.0 ms {}",
            "2026-11-17 10:00:02.000 +00:00 [ERR] HTTP POST /failed responded 500 in 120.0 ms {}");

        var slowSuccess = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            HttpStatusCode = 200,
            MinDurationMs = 100
        });
        var serverErrors = await CreateService().QueryPageAsync(new ApplicationLogQuery
        {
            HttpStatusCode = 500,
            MinDurationMs = 100,
            MaxDurationMs = 200
        });

        Assert.Equal("/slow", Assert.Single(slowSuccess.Items).RequestPath);
        Assert.Equal("/failed", Assert.Single(serverErrors.Items).RequestPath);
        Assert.Equal(1, slowSuccess.TotalItems);
        Assert.Equal(1, serverErrors.TotalItems);
    }

    private ApplicationLogQueryService CreateService() =>
        new(new TestHostEnvironment { ContentRootPath = _root },
            NullLogger<ApplicationLogQueryService>.Instance,
            new MemoryCache(new MemoryCacheOptions()));

    private void WriteLog(params string[] lines) =>
        File.WriteAllLines(Path.Combine(_root, "logs", "serilog", "serilog-20261117.txt"), lines);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
