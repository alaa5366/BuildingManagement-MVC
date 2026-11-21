// =====================================================================
//  AuthResult — نتيجة عملية تسجيل الدخول
//  بيستخدم في:
//    - IdentityAccountAuthenticationService
//    - AccountController
//    - ImpersonationController
// =====================================================================
namespace BuildingManagementMvc.Services;

/// <summary>A currently active resident occupancy authorized by an active Resident role assignment.</summary>
public sealed class ResidentLoginScope
{
    /// <summary>Gets or sets the authorized building identifier.</summary>
    public string BuildingId { get; set; } = "";
    /// <summary>Gets or sets the active apartment identifier.</summary>
    public string ApartmentId { get; set; } = "";
}

/// <summary>
/// Outcome of a sign-in attempt: success with the identity data, or failure with a reason code.
/// </summary>
public class AuthResult
{
    /// <summary>
    /// Gets or sets a value indicating whether it is success.
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// Gets or sets the error.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets the reason.
    /// </summary>
    public string? Reason { get; set; }
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? Uid { get; set; }
    /// <summary>
    /// Gets or sets the role.
    /// </summary>
    public string? Role { get; set; }
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>Canonical phone identifier for resident PIN-change continuation.</summary>
    public string? Phone { get; set; }
    /// <summary>Marks a principal issued from the target identity schema.</summary>
    public bool UsesTargetIdentity { get; set; }
    /// <summary>Requires replacement of a temporary initial resident PIN before access is granted.</summary>
    public bool MustChangePin { get; set; }
    /// <summary>Target account security stamp for session revalidation.</summary>
    public string? SecurityStamp { get; set; }
    /// <summary>
    /// Gets or sets the building IDs.
    /// </summary>
    public List<string> BuildingIds { get; set; } = new();
    /// <summary>Explicit permission keys loaded from the authoritative account grant store.</summary>
    public List<string> Permissions { get; set; } = new();
    /// <summary>Every active apartment scope authorized for this resident identity at authentication time.</summary>
    public List<ResidentLoginScope> ResidentScopes { get; set; } = new();
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string? ApartmentId { get; set; }
    /// <summary>
    /// Gets or sets the apartment number.
    /// </summary>
    public int? ApartmentNumber { get; set; }

    /// <summary>
    /// Returns the ok.
    /// </summary>
    public static AuthResult Ok(
        string uid,
        string role,
        string email,
        string name,
        List<string> buildingIds,
        string? aptId = null,
        int? aptNumber = null,
        List<string>? permissions = null) => new()
        {
            Success = true,
            Uid = uid,
            Role = role,
            Email = email,
            Name = name,
            BuildingIds = buildingIds,
            Permissions = permissions ?? new List<string>(),
            ApartmentId = aptId,
            ApartmentNumber = aptNumber
        };

    /// <summary>
    /// Returns the fail.
    /// </summary>
    public static AuthResult Fail(string error, string? reason = null) =>
        new() { Success = false, Error = error, Reason = reason };
}
