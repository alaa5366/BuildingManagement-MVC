using System.ComponentModel.DataAnnotations;

namespace BuildingManagementMvc.Models;

public class SuperAdminLoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class AdminLoginVm
{
    [Required]
    public string BuildingId { get; set; } = "";

    [Required]
    public string Phone { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Pin { get; set; } = "";
}

public class ResidentLoginVm
{
    [Required]
    public string BuildingId { get; set; } = "";

    [Required, Range(0, 200)]
    public int FloorOrder { get; set; }

    [Required, Range(1, 999)]
    public int AptNumber { get; set; }

    [Required]
    public string Whatsapp { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Pin { get; set; } = "";
}
