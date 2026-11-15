using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class DvrNetworkPolicyTests
{
    [Theory]
    [InlineData("192.168.1.10", 554, true)]
    [InlineData("10.0.0.5", 8554, true)]
    [InlineData("172.20.0.9", 554, true)]
    [InlineData("127.0.0.1", 554, false)]
    [InlineData("169.254.169.254", 80, false)]
    [InlineData("8.8.8.8", 554, false)]
    [InlineData("localhost", 554, false)]
    [InlineData("192.168.1.10", 0, false)]
    public void IsAllowedEndpoint_RestrictsToPrivateIpLiterals(string host, int port, bool expected)
        => Assert.Equal(expected, DvrNetworkPolicy.IsAllowedEndpoint(host, port));

    [Theory]
    [InlineData("Streaming/Channels/101", true)]
    [InlineData("cam/realmonitor?channel=1&subtype=0", true)]
    [InlineData("http://127.0.0.1/admin", false)]
    [InlineData("../../etc/passwd", false)]
    [InlineData("foo\r\nHost: evil", false)]
    public void IsSafeRelativeRtspPath_RejectsAbsoluteAndTraversalPaths(string path, bool expected)
        => Assert.Equal(expected, DvrNetworkPolicy.IsSafeRelativeRtspPath(path));
}
