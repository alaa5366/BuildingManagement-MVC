namespace BuildingManagementMvc.Models;

/// <summary>
/// An admin or super admin account as used by the application layer (mapped to <see cref="BuildingManagementMvc.Data.Entities.UserEntity"/>).
/// </summary>
public class AppUserDoc
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string Uid { get; set; } = "";
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string Email { get; set; } = "";
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the phone.
    /// </summary>
    public string Phone { get; set; } = "";
    /// <summary>
    /// Gets or sets the WhatsApp.
    /// </summary>
    public string Whatsapp { get; set; } = "";
    /// <summary>
    /// Gets or sets the PIN.
    /// </summary>
    public string Pin { get; set; } = "";
    /// <summary>
    /// Gets or sets the photo URL.
    /// </summary>
    public string PhotoUrl { get; set; } = "";
    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    public string Role { get; set; } = "";
    /// <summary>
    /// Gets or sets the building IDs.
    /// </summary>
    public List<string> BuildingIds { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether it is disabled.
    /// </summary>
    public bool Disabled { get; set; }
    /// <summary>
    /// Gets or sets the disabled reason.
    /// </summary>
    public string DisabledReason { get; set; } = "";
    /// <summary>
    /// Gets or sets the permissions.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether it is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Gets or sets the last login date and time.
    /// </summary>
    public string? LastLoginAt { get; set; }
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string? CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string? CreatedBy { get; set; }
    /// <summary>
    /// Gets or sets the updated date and time.
    /// </summary>
    public string? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the updated by.
    /// </summary>
    public string? UpdatedBy { get; set; }
}