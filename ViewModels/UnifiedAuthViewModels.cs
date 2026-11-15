using System.ComponentModel.DataAnnotations;

namespace BuildingManagementMvc.Models;

// ============================================================
// الدخول الموحد
// ============================================================
/// <summary>
/// Form model of the unified login (identifier plus password or PIN).
/// </summary>
public class UnifiedLoginVm
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    [Required(ErrorMessage = "Validation_IdentifierRequired")]
    public string Identifier { get; set; } = "";

    /// <summary>
    /// Gets or sets the credential.
    /// </summary>
    [Required(ErrorMessage = "Validation_CredentialRequired")]
    [DataType(DataType.Password)]
    public string Credential { get; set; } = "";
}

// ============================================================
// صفحة اختيار الدور (لو المستخدم عنده أكتر من دور)
// ============================================================
/// <summary>
/// Model of the account selection page.
/// </summary>
public class ChooseContextVm
{
    /// <summary>
    /// Gets or sets the full name.
    /// </summary>
    public string FullName { get; set; } = "";
    /// <summary>
    /// Gets or sets the contexts.
    /// </summary>
    public List<UserContext> Contexts { get; set; } = new();

    // التوكن المؤقت (لحفظ بيانات الدخول بين الـ requests)
    /// <summary>
    /// Gets or sets the session token.
    /// </summary>
    public string SessionToken { get; set; } = "";
}

// ============================================================
// سياق المستخدم (دور + عمارة/شقة)
// ============================================================
/// <summary>
/// One account (admin of a building or resident of an apartment) that the same phone number or e-mail can enter.
/// </summary>
public class UserContext
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";            // معرّف فريد للسياق
    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    public string Type { get; set; } = "";           // "superadmin" | "admin" | "resident"
    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    public string Icon { get; set; } = "";           // 👑 / 🏢 / 🏠
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = "";          // "Super Admin" / "أدمن" / "ساكن"
    /// <summary>
    /// Gets or sets the subtitle.
    /// </summary>
    public string Subtitle { get; set; } = "";       // اسم العمارة / رقم الشقة

    // بيانات تقنية
    /// <summary>
    /// Gets or sets the identifier of the building.
    /// </summary>
    public string? BuildingId { get; set; }
    /// <summary>
    /// Gets or sets the building name.
    /// </summary>
    public string? BuildingName { get; set; }
    /// <summary>
    /// Gets or sets the building number.
    /// </summary>
    public string? BuildingNumber { get; set; }
    /// <summary>
    /// Gets or sets the floor order.
    /// </summary>
    public int? FloorOrder { get; set; }
    /// <summary>
    /// Gets or sets the apartment number.
    /// </summary>
    public int? AptNumber { get; set; }
    /// <summary>
    /// Gets or sets the apartment label.
    /// </summary>
    public string? AptLabel { get; set; }
    /// <summary>
    /// Gets or sets the email.
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string? Uid { get; set; }
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the apartment.
    /// </summary>
    public string? AptId { get; set; }

}