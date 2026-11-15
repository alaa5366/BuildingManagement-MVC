namespace BuildingManagementMvc.Models;

/// <summary>
/// An in-app notification addressed to a user or an apartment.
/// </summary>
public class Notification
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public string Ts { get; set; } = "";
    /// <summary>
    /// Gets or sets the recipient type.
    /// </summary>
    public string RecipientType { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the recipient.
    /// </summary>
    public string? RecipientId { get; set; }
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string Type { get; set; } = "info";
    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    public string Icon { get; set; } = "🔔";
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = "";
    /// <summary>
    /// Gets or sets the body.
    /// </summary>
    public string Body { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is read.
    /// </summary>
    public bool Read { get; set; }
    /// <summary>
    /// Gets or sets the read date and time.
    /// </summary>
    public string? ReadAt { get; set; }
}