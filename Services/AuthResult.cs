// =====================================================================
//  AuthResult — نتيجة عملية تسجيل الدخول
//  بيستخدم في:
//    - SqlAuthService
//    - AccountController
//    - ImpersonationController
// =====================================================================
namespace BuildingManagementMvc.Services;

public class AuthResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Reason { get; set; }
    public string? Uid { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public List<string> BuildingIds { get; set; } = new();
    public string? ApartmentId { get; set; }
    public int? ApartmentNumber { get; set; }

    public static AuthResult Ok(
        string uid,
        string role,
        string email,
        string name,
        List<string> buildingIds,
        string? aptId = null,
        int? aptNumber = null) => new()
        {
            Success = true,
            Uid = uid,
            Role = role,
            Email = email,
            Name = name,
            BuildingIds = buildingIds,
            ApartmentId = aptId,
            ApartmentNumber = aptNumber
        };

    public static AuthResult Fail(string error, string? reason = null) =>
        new() { Success = false, Error = error, Reason = reason };
}