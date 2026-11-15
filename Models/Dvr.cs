namespace BuildingManagementMvc.Models;

/// <summary>
/// A DVR device as used by the application layer.
/// </summary>
public class DvrDoc
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string BuildingId { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the IP address address.
    /// </summary>
    public string IpAddress { get; set; } = "";
    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    public int Port { get; set; } = 554;
    /// <summary>
    /// Gets or sets the brand.
    /// </summary>
    public string Brand { get; set; } = "";
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string CreatedAt { get; set; } = "";
}