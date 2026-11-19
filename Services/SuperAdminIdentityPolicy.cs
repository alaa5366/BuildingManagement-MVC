namespace BuildingManagementMvc.Services;

/// <summary>Centralizes the configured SuperAdmin allow-list and session-stamp policy.</summary>
public static class SuperAdminIdentityPolicy
{
    private static string[] _allowedEmails = Array.Empty<string>();

    /// <summary>Configures the e-mail addresses permitted to authenticate as SuperAdmin.</summary>
    public static void Configure(string? commaSeparated)
    {
        _allowedEmails = (commaSeparated ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Determines whether an e-mail address is on the configured SuperAdmin allow-list.</summary>
    public static bool IsEmailAllowed(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized.Length > 0 && _allowedEmails.Contains(normalized, StringComparer.Ordinal);
    }

    /// <summary>Creates a non-reversible session version from credential and account status evidence.</summary>
    public static string CreateSecurityStamp(
        string? credentialHash, DateTime? updatedAt, bool isActive, bool isDisabled)
    {
        var material = string.Join("|", credentialHash ?? string.Empty, updatedAt?.Ticks ?? 0, isActive, isDisabled);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(material)));
    }
}
