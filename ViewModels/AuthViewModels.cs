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

    [Required]
    public string FloorId { get; set; } = "";

    [Required]
    public string AptId { get; set; } = "";

    [Required]
    public string Whatsapp { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Pin { get; set; } = "";
}