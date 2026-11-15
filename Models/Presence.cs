namespace BuildingManagementMvc.Models;

/// <summary>
/// Last-seen presence information of a user.
/// </summary>
public class Presence
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Uid { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string AptId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the last seen.
    /// </summary>
    public string LastSeen { get; set; } = "";
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public long Ts { get; set; }
}