namespace BuildingManagementMvc.Models;

/// <summary>
/// Usage counter of a QR token as used by the application layer.
/// </summary>
public class QrUsageDoc
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the token.
    /// </summary>
    public string TokenId { get; set; } = "";
    /// <summary>
    /// Gets or sets the use count.
    /// </summary>
    public int UseCount { get; set; }
    /// <summary>
    /// Gets or sets the first used date and time.
    /// </summary>
    public string FirstUsedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the last used date and time.
    /// </summary>
    public string LastUsedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the device fingerprint.
    /// </summary>
    public string? DeviceFingerprint { get; set; }
}