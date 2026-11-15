using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

// ================================================================
// PresenceService — Static Methods
// ================================================================
public class PresenceServiceStaticTests
{
    [Fact]
    public void IsOnline_NullPresence_ReturnsFalse()
    {
        Assert.False(PresenceService.IsOnline(null));
    }

    [Fact]
    public void IsOnline_ZeroTs_ReturnsFalse()
    {
        var p = new Presence { Ts = 0 };
        Assert.False(PresenceService.IsOnline(p));
    }

    [Fact]
    public void IsOnline_JustNow_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var p = new Presence { Ts = now };
        Assert.True(PresenceService.IsOnline(p));
    }

    [Fact]
    public void IsOnline_OldTimestamp_ReturnsFalse()
    {
        var old = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();
        var p = new Presence { Ts = old };
        Assert.False(PresenceService.IsOnline(p));
    }

    [Fact]
    public void IsRecent_JustNow_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var p = new Presence { Ts = now };
        Assert.True(PresenceService.IsRecent(p));
    }

    [Fact]
    public void IsRecent_25HoursAgo_ReturnsFalse()
    {
        var old = DateTimeOffset.UtcNow.AddHours(-25).ToUnixTimeMilliseconds();
        var p = new Presence { Ts = old };
        Assert.False(PresenceService.IsRecent(p));
    }

    [Fact]
    public void TimeAgo_NullPresence_ReturnsNeverLoggedIn()
    {
        var result = PresenceService.TimeAgo(null);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void TimeAgo_JustNow_ReturnsNow()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var p = new Presence { Ts = now };
        var result = PresenceService.TimeAgo(p);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void BuildDocId_WithBuildingAndApt_CombinesThem()
    {
        var docId = PresenceService.BuildDocId("u1", "a1", "b1");
        Assert.Equal("b1_a1", docId);
    }

    [Fact]
    public void BuildDocId_WithoutBuildingOrApt_ReturnsUid()
    {
        Assert.Equal("u1", PresenceService.BuildDocId("u1", "", "b1"));
        Assert.Equal("u1", PresenceService.BuildDocId("u1", "a1", ""));
        Assert.Equal("u1", PresenceService.BuildDocId("u1", "", ""));
    }
}

// ================================================================
// QrGeneratorService
// ================================================================
public class QrGeneratorServiceTests
{
    private readonly QrGeneratorService _service = new();

    [Fact]
    public void GeneratePng_ValidText_ReturnsNonEmptyBytes()
    {
        var bytes = _service.GeneratePng("https://example.com");
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
        // PNG signature: 89 50 4E 47
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }

    [Fact]
    public void GeneratePng_DifferentTexts_ReturnDifferentBytes()
    {
        var bytes1 = _service.GeneratePng("https://example.com/1");
        var bytes2 = _service.GeneratePng("https://example.com/2");
        Assert.NotEqual(bytes1.Length, bytes2.Length);
    }

    [Fact]
    public void GetApartmentLoginUrl_BuildsCorrectFormat()
    {
        var url = _service.GetApartmentLoginUrl("https://example.com", "b1", 0, 5);
        Assert.Contains("/Account/LoginResident", url);
        Assert.Contains("buildingId=b1", url);
        Assert.Contains("floorOrder=0", url);
        Assert.Contains("aptNumber=5", url);
    }

    [Fact]
    public void GetApartmentLoginUrl_HandlesTrailingSlash()
    {
        var url = _service.GetApartmentLoginUrl("https://example.com/", "b1", 0, 5);
        Assert.StartsWith("https://example.com/Account", url);
        Assert.DoesNotContain("//Account", url);
    }
}

// ================================================================
// MediaMtxService — Static Methods
// ================================================================
public class MediaMtxServiceStaticTests
{
    [Fact]
    public void BuildRtspUrl_WithAuth_IncludesCredentials()
    {
        var url = MediaMtxService.BuildRtspUrl("192.168.1.100", 554, "admin", "pass", "stream");
        Assert.Equal("rtsp://admin:pass@192.168.1.100:554/stream", url);
    }

    [Fact]
    public void BuildRtspUrl_NoAuth_ExcludesCredentials()
    {
        var url = MediaMtxService.BuildRtspUrl("192.168.1.100", 554, "", "", "stream");
        Assert.Equal("rtsp://192.168.1.100:554/stream", url);
    }

    [Fact]
    public void BuildRtspUrl_EscapesCredentials()
    {
        var url = MediaMtxService.BuildRtspUrl("192.168.1.100", 554, "user@domain", "pa:ss", "stream");
        Assert.Contains("user%40domain", url);
        Assert.Contains("pa%3Ass", url);
    }

    [Fact]
    public void BuildRtspUrl_StripsLeadingSlashFromPath()
    {
        var url = MediaMtxService.BuildRtspUrl("192.168.1.100", 554, "", "", "/stream");
        Assert.Equal("rtsp://192.168.1.100:554/stream", url);
    }

    [Theory]
    [InlineData("hikvision", 1, "Streaming/Channels/101")]
    [InlineData("dahua", 2, "cam/realmonitor?channel=2&subtype=0")]
    [InlineData("onvif", 1, "onvif1")]
    [InlineData("unknown", 3, "ch3")]
    public void GetDefaultRtspPath_ReturnsBrandSpecificPath(string brand, int channel, string expected)
    {
        Assert.Equal(expected, MediaMtxService.GetDefaultRtspPath(brand, channel));
    }

    [Fact]
    public void GetDefaultRtspPath_NullBrand_ReturnsDefault()
    {
        Assert.Equal("ch1", MediaMtxService.GetDefaultRtspPath(null!, 1));
    }
}