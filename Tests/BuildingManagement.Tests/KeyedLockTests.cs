using BuildingManagementMvc.Data;
using Xunit;

namespace BuildingManagement.Tests;

public class KeyedLockTests
{
    [Fact]
    public async Task Same_key_is_serialized()
    {
        var key = "building:test-" + Guid.NewGuid();
        var first = await KeyedLock.AcquireAsync(key);

        var second = KeyedLock.AcquireAsync(key);
        await Task.Delay(150);
        Assert.False(second.IsCompleted);      // لسه مستني الأول

        first.Dispose();
        using var _ = await second.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(second.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Different_keys_do_not_block_each_other()
    {
        using var a = await KeyedLock.AcquireAsync("building:A-" + Guid.NewGuid());
        using var b = await KeyedLock.AcquireAsync("building:B-" + Guid.NewGuid()).WaitAsync(TimeSpan.FromSeconds(2));
        Assert.NotNull(b);
    }
}
