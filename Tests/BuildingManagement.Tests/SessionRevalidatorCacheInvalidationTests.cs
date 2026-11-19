using System.Collections.Concurrent;
using System.Reflection;
using BuildingManagementMvc.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public class SessionRevalidatorCacheInvalidationTests
{
    [Fact]
    public void InvalidateAdmin_AdvancesPermissionCacheVersion()
    {
        var sut = new SessionRevalidator(
            sqlFactory: null!,
            cache: new MemoryCache(new MemoryCacheOptions()),
            logger: NullLogger<SessionRevalidator>.Instance);

        var field = typeof(SessionRevalidator).GetField(
            "_permissionVersions",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        var versions = Assert.IsType<ConcurrentDictionary<string, long>>(field!.GetValue(sut));

        Assert.Equal(0, versions.GetOrAdd("admin-1", 0));

        sut.InvalidateAdmin("admin-1");
        Assert.Equal(1, versions["admin-1"]);

        sut.InvalidateAdmin("admin-1");
        Assert.Equal(2, versions["admin-1"]);
    }

    [Fact]
    public void InvalidateAdmin_IgnoresBlankUserIds()
    {
        var sut = new SessionRevalidator(
            sqlFactory: null!,
            cache: new MemoryCache(new MemoryCacheOptions()),
            logger: NullLogger<SessionRevalidator>.Instance);

        var field = typeof(SessionRevalidator).GetField(
            "_permissionVersions",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(field);
        var versions = Assert.IsType<ConcurrentDictionary<string, long>>(field!.GetValue(sut));

        sut.InvalidateAdmin("");
        sut.InvalidateAdmin(" ");

        Assert.Empty(versions);
    }
}
