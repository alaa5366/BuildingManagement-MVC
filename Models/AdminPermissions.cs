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
    // ✅ قالب Super — كل الصلاحيات (22) — للاستخدام الحذر
    public static readonly List<string> TemplateAdminSuper = new()
{
    // إدارة
    ManageBuildings, ManageAdmins, ManageResidents,

    // مالية
    ManageWallet, ManageExpenses, ManageRevenues,
    ManageCategories, ViewReports,

    // خدمات
    ManagePolls, ManageMaintenance, SendNotifications, UseWhatsApp,

    // نظام
    ViewAuditLogAll, ViewAuditLogBuilding, ViewAuditLogOwn,
    ManageSettingsGlobal, ManageSettingsBuilding, ManageSettingsOwn,
    ManageTools, ManageBackup, ManageSync, SecretAccess
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
    // ============================================================
    // التصنيفات (Categories)
    // ============================================================
    public enum PermissionCategory
    {
        Admin,      // إدارة
        Financial,  // مالية
        Services,   // خدمات
        System      // نظام
    }

    public static PermissionCategory CategoryOf(string key) => key switch
    {
        ManageBuildings or ManageAdmins or ManageResidents => PermissionCategory.Admin,
        ManageWallet or ManageExpenses or ManageRevenues or ManageCategories or ViewReports => PermissionCategory.Financial,
        ManagePolls or ManageMaintenance or SendNotifications or UseWhatsApp => PermissionCategory.Services,
        ViewAuditLogAll or ViewAuditLogBuilding or ViewAuditLogOwn => PermissionCategory.System,
        ManageSettingsGlobal or ManageSettingsBuilding or ManageSettingsOwn => PermissionCategory.System,
        ManageTools or ManageBackup or ManageSync or SecretAccess => PermissionCategory.System,
        _ => PermissionCategory.System
    };

    public static string CategoryLabelAr(PermissionCategory cat) => cat switch
    {
        PermissionCategory.Admin => "🏢 إدارة",
        PermissionCategory.Financial => "💰 مالية",
        PermissionCategory.Services => "🛎️ خدمات",
        PermissionCategory.System => "🛠️ نظام",
        _ => cat.ToString()
    };

    // ============================================================
    // الوصف التفصيلي
    // ============================================================
    public static string DescriptionAr(string key) => key switch
    {
        ManageBuildings => "إنشاء/تعديل/حذف العمارات + إدارة الهيكل (أدوار + شقق)",
        ManageAdmins => "إضافة/تعديل/تعطيل الأدمنة + إدارة الصلاحيات",
        ManageResidents => "إضافة/تعديل/تعطيل السكان + إعادة تعيين PIN",

        ManageWallet => "عرض الأرصدة، تأكيد/إلغاء الدفعات، التسويات اليدوية، إصدار الفواتير",
        ManageExpenses => "إضافة/تعديل/حذف المصروفات الشهرية",
        ManageRevenues => "إضافة/تعديل/حذف الإيرادات الشهرية",
        ManageCategories => "إدارة فئات المصروفات والإيرادات (ألوان، ترتيب، تفعيل)",
        ViewReports => "عرض التقارير المالية + تصدير CSV",

        ManagePolls => "إنشاء/إغلاق/حذف التصويتات + متابعة النتائج",
        ManageMaintenance => "إضافة/تعديل/حذف سجلات الصيانة + إضافة التكلفة للمصروفات",
        SendNotifications => "إرسال إشعارات داخل التطبيق للأدمنة والسكان",
        UseWhatsApp => "توليد رسائل واتساب من القوالب + فتح روابط wa.me",

        ViewAuditLogAll => "عرض كل السجلات في كل العمارات (Super Admin)",
        ViewAuditLogBuilding => "عرض سجل العمارة فقط (Admin)",
        ViewAuditLogOwn => "عرض سجل شقتي فقط (Resident)",

        ManageSettingsGlobal => "إعدادات النظام العامة (Super Admin)",
        ManageSettingsBuilding => "إعدادات العمارة (سياسة فواتير، توزيع مصروفات، …)",
        ManageSettingsOwn => "إعداداتي الشخصية (موبايل، واتساب، PIN، اللغة)",

        ManageTools => "Migration + صيانة DB + تصدير شامل ZIP",
        ManageBackup => "نسخ احتياطي كامل + استعادة",
        ManageSync => "مزامنة Firebase Auth مع Firestore (فحص/إصلاح)",
        SecretAccess => "الدخول السري (impersonation) للعمارات والشقق",

        _ => key
    };

    // ============================================================
    // Service + Controller + Actions
    // ============================================================
    public static (string Service, string Controller, string Actions) BackendInfo(string key) => key switch
    {
        ManageBuildings => ("BuildingsService", "BuildingsController",
            "Index, Create, Details, QrCodes, QrCodesPdf, Delete, AddFloor, AddApartment, FixFirebaseAccounts, RecreateFirebaseAccounts, SyncFirebasePasswords"),

        ManageAdmins => ("AdminManagementService", "AdminsController, PermissionsController",
            "Index, Create, Edit, ChangePin, Deactivate, Activate, Delete, Permissions.Index, Permissions.Toggle"),

        ManageResidents => ("BuildingsService", "AdminAptsController",
            "Index, GenerateQuickQr"),

        ManageWallet => ("WalletService", "WalletController",
            "Index, ConfirmDeposit, CancelDeposit, AddAdjustment"),

        ManageExpenses => ("ExpensesService", "ExpensesController",
            "Index, Add, Delete"),

        ManageRevenues => ("RevenuesService", "RevenuesController",
            "Index, Add, Delete"),

        ManageCategories => ("CategoriesService", "CategoriesController",
            "Index, Manage, Add, Update, Delete, Reorder"),

        ViewReports => ("ReportsService", "ReportsController",
            "Index, ExportCsv"),

        ManagePolls => ("PollsService", "PollsController, ResidentPollsController",
            "Index, Create, Close, Reopen, Delete, Vote"),

        ManageMaintenance => ("MaintenanceService", "MaintenanceController, ResidentMaintenanceController",
            "Index, Add, Update, Delete, AddToExpenses"),

        SendNotifications => ("NotificationsService", "NotificationsController",
            "Index, MarkRead, MarkAllRead"),

        UseWhatsApp => ("WhatsAppTemplateService", "WhatsAppController",
            "Index, Generate"),

        ViewAuditLogAll => ("AuditLogQueryService", "AuditLogController",
            "Index (Super Admin — كل السجلات), ExportCsv"),

        ViewAuditLogBuilding => ("AuditLogQueryService", "AuditLogController",
            "Index (Admin — سجل عمارته)"),

        ViewAuditLogOwn => ("AuditLogQueryService", "AuditLogController",
            "Index (Resident — سجل شقته)"),

        ManageSettingsGlobal => ("(قريبًا) SettingsService", "(قريبًا) SettingsController", "-"),
        ManageSettingsBuilding => ("(قريبًا) SettingsService", "(قريبًا) SettingsController", "-"),
        ManageSettingsOwn => ("(قريبًا) SettingsService", "(قريبًا) SettingsController", "-"),

        ManageTools => ("MigrationService, DbMaintenanceService", "ToolsController",
            "Index, Migration, RunMigration, Maintenance, RunMaintenance, Export, ExportAll"),

        ManageBackup => ("(قريبًا) BackupService", "(قريبًا) BackupController", "-"),

        ManageSync => ("DbSyncService", "SyncController",
            "Index, SyncAdmins, SyncResidents, SyncPasswords"),

        SecretAccess => ("(قريبًا) SecretAccessService", "(قريبًا) SecretAccessController", "-"),

        _ => ("-", "-", "-")
    };
}