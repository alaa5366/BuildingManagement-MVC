using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using Xunit;

namespace BuildingManagement.Tests;

public class HealthCheckResponseWriterTests
{
    [Fact]
    public async Task WriteAsync_ReturnsAggregateAndComponentStatuses()
    {
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Database"] = new(
                    HealthStatus.Healthy,
                    description: null,
                    exception: null,
                    duration: TimeSpan.FromMilliseconds(4),
                    data: new Dictionary<string, object>())
            },
            TimeSpan.FromMilliseconds(4));
        var context = CreateContext();

        await HealthCheckResponseWriter.WriteAsync(context, report);

        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        var json = await ReadResponseAsync(context);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("Database", document.RootElement.GetProperty("checks")[0].GetProperty("name").GetString());
        Assert.Equal("Healthy", document.RootElement.GetProperty("checks")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task WriteAsync_DoesNotExposeExceptionMessagesOrConnectionDetails()
    {
        const string secret = "Server=private-db;Password=do-not-disclose";
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["Database"] = new(
                    HealthStatus.Unhealthy,
                    description: secret,
                    exception: new InvalidOperationException(secret),
                    duration: TimeSpan.FromMilliseconds(8),
                    data: new Dictionary<string, object>
                    {
                        ["connectionString"] = secret
                    })
            },
            TimeSpan.FromMilliseconds(8));
        var context = CreateContext();

        await HealthCheckResponseWriter.WriteAsync(context, report);

        var json = await ReadResponseAsync(context);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("Unhealthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("Unhealthy", document.RootElement.GetProperty("checks")[0].GetProperty("status").GetString());
        Assert.DoesNotContain(secret, json, StringComparison.Ordinal);
        Assert.DoesNotContain("exception", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connectionString", json, StringComparison.OrdinalIgnoreCase);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
