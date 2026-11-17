using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
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
            "2026-11-17 10:00:00.000 +00:00 [ERR] Login failed password=super-secret user=alaa@example.com apiKey=api-secret Server=prod-db;Password=db-secret client payload {\"SourceContext\":\"Tests.Auth\"}",
            "System.InvalidOperationException: eyJabcdefghijk.eyJabcdefghijk.eyJabcdefghijk",
            "   at Tests.Auth.Run()");

        var item = Assert.Single((await CreateService().QueryPageAsync(new ApplicationLogQuery())).Items);
        Assert.True(item.HasException);
        Assert.DoesNotContain("super-secret", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("alaa@example.com", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("api-secret", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("prod-db", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("db-secret", item.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJabcdefghijk", item.Exception, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", item.Message, StringComparison.Ordinal);
        Assert.Contains("[REDACTED_TOKEN]", item.Exception, StringComparison.Ordinal);
        Assert.Contains("Tests.Auth.Run", item.Exception, StringComparison.Ordinal);
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

    private ApplicationLogQueryService CreateService() =>
        new(new TestHostEnvironment { ContentRootPath = _root },
            NullLogger<ApplicationLogQueryService>.Instance);

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
