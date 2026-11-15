namespace BuildingManagementMvc.Models;

/// <summary>
/// A stored QR access token.
/// </summary>
public class QrToken
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Uid { get; set; } = "";
    /// <summary>
    /// Gets or sets the bld.
    /// </summary>
    public string Bld { get; set; } = "";
    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public string Apt { get; set; } = "";
    /// <summary>
    /// Gets or sets the expense.
    /// </summary>
    public long Exp { get; set; }
    /// <summary>
    /// Gets or sets the use.
    /// </summary>
    public string Use { get; set; } = "single";
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public long Ts { get; set; }
    /// <summary>
    /// Gets or sets the by.
    /// </summary>
    public string By { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is used.
    /// </summary>
    public bool Used { get; set; }
    /// <summary>
    /// Gets or sets the used date and time.
    /// </summary>
    public string? UsedAt { get; set; }
    /// <summary>
    /// Gets or sets the used by.
    /// </summary>
    public string? UsedBy { get; set; }
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string CreatedAt { get; set; } = "";
}

/// <summary>
/// Payload carried by a stable (printed) access QR code.
/// </summary>
public class AccessTokenPayload
{
    /// <summary>
    /// Gets or sets the bld.
    /// </summary>
    public string Bld { get; set; } = "";
    /// <summary>
    /// Gets or sets the apartment.
    /// </summary>
    public string Apt { get; set; } = "";
    /// <summary>
    /// Gets or sets the expense.
    /// </summary>
    public long Exp { get; set; }
    /// <summary>
    /// Gets or sets the use.
    /// </summary>
    public string Use { get; set; } = "single";
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Uid { get; set; } = "";
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public long Ts { get; set; }
    /// <summary>
    /// Gets or sets the by.
    /// </summary>
    public string By { get; set; } = "";
    /// <summary>
    /// Gets or sets the sig.
    /// </summary>
    public string Sig { get; set; } = "";
}