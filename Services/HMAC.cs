using System.Security.Cryptography;
using System.Text;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Small helper that computes lowercase hex HMAC-SHA256 signatures.
/// </summary>
public static class HMAC
{
    /// <summary>
    /// Computes the operation on the HMAC.
    /// </summary>
    public static string Compute(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}