using System.Net;

namespace BuildingManagementMvc.Services;

/// <summary>Restricts camera pull sources to explicit private-network IP literals.</summary>
public static class DvrNetworkPolicy
{
    /// <summary>
    /// يتحقق مما إذا كان عنوان endpoint مسموحًا به سياسة الشبكة.
    /// </summary>
    /// <param name="host">اسم المضيف أو عنوان IP.</param>
    /// <param name="port">رقم المنفذ.</param>
    /// <returns><c>true</c> إذا كان مسموحًا؛ وإلا <c>false</c>.</returns>
    public static bool IsAllowedEndpoint(string? host, int port)
    {
        if (port is < 1 or > 65535 || !IPAddress.TryParse(host, out var address)) return false;
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal) return false;

        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        // IPv6 Unique Local Addresses (fc00::/7) only.
        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
               (bytes[0] & 0xFE) == 0xFC;
    }
    /// <summary>
    /// يتحقق مما إذا كان مسار RTSP النسبي آمنًا (لا يحتوي على اجتياز مجلدات).
    /// </summary>
    /// <param name="path">مسار RTSP النسبي.</param>
    /// <returns><c>true</c> إذا كان آمنًا؛ وإلا <c>false</c>.</returns>
    public static bool IsSafeRelativeRtspPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        if (path.Length > 256 || path.Contains('\\') || path.Contains('\r') || path.Contains('\n')) return false;
        if (Uri.TryCreate(path, UriKind.Absolute, out _)) return false;
        if (!System.Text.RegularExpressions.Regex.IsMatch(path, @"^[A-Za-z0-9_./?=&-]+$")) return false;
        return !path.Split('/').Any(segment => segment == ".." || segment == ".");
    }
}
