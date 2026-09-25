using System.ComponentModel.DataAnnotations;

namespace BuildingManagementMvc.Models;

// ============================================================
// الدخول الموحد
// ============================================================
public class UnifiedLoginVm
{
    [Required(ErrorMessage = "الإيميل أو رقم التليفون مطلوب")]
    public string Identifier { get; set; } = "";

    [Required(ErrorMessage = "الباسورد أو PIN مطلوب")]
    [DataType(DataType.Password)]
    public string Credential { get; set; } = "";
}

// ============================================================
// صفحة اختيار الدور (لو المستخدم عنده أكتر من دور)
// ============================================================
public class ChooseContextVm
{
    public string FullName { get; set; } = "";
    public List<UserContext> Contexts { get; set; } = new();

    // التوكن المؤقت (لحفظ بيانات الدخول بين الـ requests)
    public string SessionToken { get; set; } = "";
}

// ============================================================
// سياق المستخدم (دور + عمارة/شقة)
// ============================================================
public class UserContext
{
    public string Id { get; set; } = "";            // معرّف فريد للسياق
    public string Type { get; set; } = "";           // "superadmin" | "admin" | "resident"
    public string Icon { get; set; } = "";           // 👑 / 🏢 / 🏠
    public string Title { get; set; } = "";          // "Super Admin" / "أدمن" / "ساكن"
    public string Subtitle { get; set; } = "";       // اسم العمارة / رقم الشقة

    // بيانات تقنية
    public string? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string? BuildingNumber { get; set; }
    public int? FloorOrder { get; set; }
    public int? AptNumber { get; set; }
    public string? AptLabel { get; set; }
    public string? Email { get; set; }
    public string? Uid { get; set; }
    public string? Name { get; set; }
}