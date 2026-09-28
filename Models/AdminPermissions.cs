namespace BuildingManagementMvc.Models;

// ثوابت الصلاحيات (تُخزَّن في مستند users/{uid}.permissions)
public static class AdminPermissions
{
    // إدارة
    public const string ManageBuildings = "buildings.manage";
    public const string ManageAdmins = "admins.manage";
    public const string ManageResidents = "residents.manage";

    // مالية
    public const string ManageWallet = "wallet.manage";
    public const string ManageExpenses = "expenses.manage";
    public const string ManageRevenues = "revenues.manage";
    public const string ManageCategories = "categories.manage";
    public const string ViewReports = "reports.view";

    // خدمات
    public const string ManagePolls = "polls.manage";
    public const string ManageMaintenance = "maintenance.manage";
    public const string SendNotifications = "notifications.send";
    public const string UseWhatsApp = "whatsapp.use";

    // السجل
    public const string ViewAuditLogAll = "audit.view.all";
    public const string ViewAuditLogBuilding = "audit.view.building";
    public const string ViewAuditLogOwn = "audit.view.own";

    // الإعدادات
    public const string ManageSettingsGlobal = "settings.manage.global";
    public const string ManageSettingsBuilding = "settings.manage.building";
    public const string ManageSettingsOwn = "settings.manage.own";

    // أدوات
    public const string ManageTools = "tools.manage";
    public const string ManageBackup = "backup.manage";
    public const string ManageSync = "sync.manage";
    public const string SecretAccess = "secret.access";

    public static readonly List<string> All = new()
    {
        ManageBuildings, ManageAdmins, ManageResidents,
        ManageWallet, ManageExpenses, ManageRevenues, ManageCategories, ViewReports,
        ManagePolls, ManageMaintenance, SendNotifications, UseWhatsApp,
        ViewAuditLogAll, ViewAuditLogBuilding, ViewAuditLogOwn,
        ManageSettingsGlobal, ManageSettingsBuilding, ManageSettingsOwn,
        ManageTools, ManageBackup, ManageSync, SecretAccess
    };

    public static readonly List<string> TemplateAdminFull = new()
    {
        ManageResidents, ManageWallet, ManageExpenses, ManageRevenues,
        ManageCategories, ViewReports, ManagePolls, ManageMaintenance,
        SendNotifications, UseWhatsApp,
        ViewAuditLogBuilding, ManageSettingsBuilding
    };

    public static readonly List<string> TemplateAdminFinancial = new()
    {
        ManageWallet, ManageExpenses, ManageRevenues,
        ManageCategories, ViewReports,
        ViewAuditLogBuilding, ManageSettingsBuilding
    };

    public static readonly List<string> TemplateAdminMaintenance = new()
    {
        ManageMaintenance, SendNotifications,
        ViewAuditLogBuilding, ManageSettingsBuilding
    };

    public static readonly List<string> SuperAdminAll = All;

    public static readonly List<string> ResidentBasic = new()
    {
        ManageSettingsOwn, ViewAuditLogOwn
    };

    // أسماء عربية للعرض
    public static string LabelAr(string key) => key switch
    {
        ManageBuildings => "إدارة العمارات",
        ManageAdmins => "إدارة الأدمنة",
        ManageResidents => "إدارة السكان",
        ManageWallet => "إدارة المحافظ",
        ManageExpenses => "إدارة المصروفات",
        ManageRevenues => "إدارة الإيرادات",
        ManageCategories => "الفئات المالية",
        ViewReports => "عرض التقارير",
        ManagePolls => "إدارة التصويتات",
        ManageMaintenance => "إدارة الصيانة",
        SendNotifications => "إرسال الإشعارات",
        UseWhatsApp => "استخدام واتساب",
        ViewAuditLogAll => "السجل الكامل",
        ViewAuditLogBuilding => "سجل العمارة",
        ViewAuditLogOwn => "سجلي فقط",
        ManageSettingsGlobal => "الإعدادات العامة",
        ManageSettingsBuilding => "إعدادات العمارة",
        ManageSettingsOwn => "إعداداتي",
        ManageTools => "الأدوات المتقدمة",
        ManageBackup => "النسخ الاحتياطي",
        ManageSync => "مزامنة الداتابيز",
        SecretAccess => "الدخول السري",
        _ => key
    };
}