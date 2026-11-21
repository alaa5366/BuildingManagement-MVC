namespace BuildingManagementMvc.Services;

/// <summary>Defines the mandatory second-factor policy for privileged administrator roles.</summary>
public static class AdministratorMfaPolicy
{
    /// <summary>Returns whether the selected role must enroll in and use MFA.</summary>
    public static bool RequiresMfa(string? role) =>
        role is "admin" or "superadmin";
}
