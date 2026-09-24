using System.Security.Cryptography;
using System.Text;

namespace BuildingManagementMvc.Services;

public static class HMAC
{
    public static string Compute(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}