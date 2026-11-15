namespace BuildingManagementMvc.Models;

/// <summary>
/// A camera as used by the application layer.
/// </summary>
public class CameraDoc
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the DVR.
    /// </summary>
    public string DvrId { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the channel.
    /// </summary>
    public int Channel { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string CreatedAt { get; set; } = "";
}