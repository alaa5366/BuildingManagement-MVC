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
        ManageBuildings => Loc.T("Manage_Buildings"),
        ManageAdmins => Loc.T("Manage_Admins"),
        ManageResidents => Loc.T("Manage_Residents"),
        ManageWallet => Loc.T("Manage_Wallets"),
        ManageExpenses => Loc.T("Manage_Expenses"),
        ManageRevenues => Loc.T("Manage_Revenues"),
        ManageCategories => Loc.T("Financial_Categories"),
        ViewReports => Loc.T("View_Reports"),
        ManagePolls => Loc.T("Manage_Polls"),
        ManageMaintenance => Loc.T("Manage_Maintenance"),
        SendNotifications => Loc.T("Send_Notifications"),
        UseWhatsApp => Loc.T("Use_WhatsApp"),
        ViewAuditLogAll => Loc.T("Full_Log"),
        ViewAuditLogBuilding => Loc.T("Building_Log"),
        ViewAuditLogOwn => Loc.T("My_Log_Only"),
        ManageSettingsGlobal => Loc.T("Global_Settings"),
        ManageSettingsBuilding => Loc.T("Building_Settings"),
        ManageSettingsOwn => Loc.T("My_Settings"),
        ManageTools => Loc.T("Advanced_Tools"),
        ManageBackup => Loc.T("Backup"),
        ManageSync => Loc.T("Database_Sync"),
        SecretAccess => Loc.T("Secret_Access"),
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
        PermissionCategory.Admin => Loc.T("Management"),
        PermissionCategory.Financial => Loc.T("Finance_2"),
        PermissionCategory.Services => Loc.T("Services"),
        PermissionCategory.System => Loc.T("System"),
        _ => cat.ToString()
    };

    // ============================================================
    // الوصف التفصيلي
    // ============================================================
    public static string DescriptionAr(string key) => key switch
    {
        ManageBuildings => Loc.T("Create_Edit_Delete_Buildings_Manage_The"),
        ManageAdmins => Loc.T("Add_Edit_Disable_Admins_Manage_Permissions"),
        ManageResidents => Loc.T("Add_Edit_Disable_Residents_Reset_PINs"),

        ManageWallet => Loc.T("View_Balances_Confirm_Cancel_Payments_Manual"),
        ManageExpenses => Loc.T("Add_Edit_Delete_Monthly_Expenses"),
        ManageRevenues => Loc.T("Add_Edit_Delete_Monthly_Revenues"),
        ManageCategories => Loc.T("Manage_Expense_And_Revenue_Categories_Colors"),
        ViewReports => Loc.T("View_Financial_Reports_CSV_Export"),

        ManagePolls => Loc.T("Create_Close_Delete_Polls_Follow_Results"),
        ManageMaintenance => Loc.T("Add_Edit_Delete_Maintenance_Records_Add"),
        SendNotifications => Loc.T("Send_In_App_Notifications_To_Admins"),
        UseWhatsApp => Loc.T("Generate_WhatsApp_Messages_From_Templates_Open"),

        ViewAuditLogAll => Loc.T("View_All_Records_Across_All_Buildings"),
        ViewAuditLogBuilding => Loc.T("View_The_Building_Log_Only_Admin"),
        ViewAuditLogOwn => Loc.T("View_My_Apartment_S_Log_Only"),

        ManageSettingsGlobal => Loc.T("Global_System_Settings_Super_Admin"),
        ManageSettingsBuilding => Loc.T("Building_Settings_Invoice_Policy_Expense_Distrib"),
        ManageSettingsOwn => Loc.T("My_Personal_Settings_Mobile_WhatsApp_PIN"),

        ManageTools => Loc.T("Migration_DB_Maintenance_Full_ZIP_Export"),
        ManageBackup => Loc.T("Full_Backup_Restore"),
        ManageSync => Loc.T("Sync_Firebase_Auth_With_Firestore_Check"),
        SecretAccess => Loc.T("Secret_Access_Impersonation_To_Buildings_And"),

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
            Loc.T("Index_Super_Admin_All_Records_ExportCsv")),

        ViewAuditLogBuilding => ("AuditLogQueryService", "AuditLogController",
            Loc.T("Index_Admin_Own_Building_S_Log")),

        ViewAuditLogOwn => ("AuditLogQueryService", "AuditLogController",
            Loc.T("Index_Resident_Own_Apartment_S_Log")),

        ManageSettingsGlobal => (Loc.T("Soon_SettingsService"), Loc.T("Soon_SettingsController"), "-"),
        ManageSettingsBuilding => (Loc.T("Soon_SettingsService"), Loc.T("Soon_SettingsController"), "-"),
        ManageSettingsOwn => (Loc.T("Soon_SettingsService"), Loc.T("Soon_SettingsController"), "-"),

        ManageTools => ("MigrationService, DbMaintenanceService", "ToolsController",
            "Index, Migration, RunMigration, Maintenance, RunMaintenance, Export, ExportAll"),

        ManageBackup => (Loc.T("Soon_BackupService"), Loc.T("Soon_BackupController"), "-"),

        ManageSync => ("DbSyncService", "SyncController",
            "Index, SyncAdmins, SyncResidents, SyncPasswords"),

        SecretAccess => (Loc.T("Soon_SecretAccessService"), Loc.T("Soon_SecretAccessController"), "-"),

        _ => ("-", "-", "-")
    };
}