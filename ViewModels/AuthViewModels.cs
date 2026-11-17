using System.ComponentModel.DataAnnotations;

namespace BuildingManagementMvc.Models;

/// <summary>
/// Form model of the super admin login.
/// </summary>
public class SuperAdminLoginVm
{
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

/// <summary>
/// Form model of the admin login.
/// </summary>
public class AdminLoginVm
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    [Required]
    public string BuildingId { get; set; } = "";

    /// <summary>
    /// Gets or sets the phone.
    /// </summary>
    [Required]
    public string Phone { get; set; } = "";

    /// <summary>
    /// Gets or sets the PIN.
    /// </summary>
    [Required, DataType(DataType.Password)]
    public string Pin { get; set; } = "";
}

/// <summary>
/// Form model of the resident login.
/// </summary>
public class ResidentLoginVm
{
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    [Required]
    public string BuildingId { get; set; } = "";

    /// <summary>
    /// Gets or sets the identifier of the floor.
    /// </summary>
    [Required]
    public string FloorId { get; set; } = "";

    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    [Required]
    public string AptId { get; set; } = "";

    /// <summary>
    /// Gets or sets the WhatsApp.
    /// </summary>
    [Required]
    public string Whatsapp { get; set; } = "";

    /// <summary>
    /// Gets or sets the PIN.
    /// </summary>
    [Required, DataType(DataType.Password)]
    public string Pin { get; set; } = "";
}
/// <summary>Form model for a six-digit authenticator code or one-time recovery code.</summary>
public class TwoFactorVerifyVm
{
    [Required]
    [StringLength(32, MinimumLength = 6)]
    public string Code { get; set; } = "";
}
