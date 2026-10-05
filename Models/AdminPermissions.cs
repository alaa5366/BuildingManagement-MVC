namespace BuildingManagementMvc.Models;

public static class AdminPermissions
{
    // ============================================================
    // إدارة
    // ============================================================
    public const string ManageBuildings = "buildings.manage";
    public const string ManageAdmins = "admins.manage";
    public const string ManageResidents = "residents.manage";
    public const string DisableApartment = "apartment.disable";

    // ============================================================
    // الكاميرات و DVR ✅ جديد
    // ============================================================
    public const string ManageDvrs = "dvrs.manage";
    public const string ViewCameras = "cameras.view";
    public const string ManageCameras = "cameras.manage";

    // ============================================================
    // استيراد ✅ جديد
    // ============================================================
    public const string ImportTemplate = "template.import";

    // ============================================================
    // مالية
    // ============================================================
    public const string ManageWallet = "wallet.manage";
    public const string ManageExpenses = "expenses.manage";
    public const string ManageRevenues = "revenues.manage";
    public const string ManageCategories = "categories.manage";
    public const string ViewReports = "reports.view";

    // ============================================================
    // خدمات
    // ============================================================
    public const string ManagePolls = "polls.manage";
    public const string ManageMaintenance = "maintenance.manage";
    public const string SendNotifications = "notifications.send";
    public const string UseWhatsApp = "whatsapp.use";

    // ============================================================
    // السجل
    // ============================================================
    public const string ViewAuditLogAll = "audit.view.all";
    public const string ViewAuditLogBuilding = "audit.view.building";
    public const string ViewAuditLogOwn = "audit.view.own";

    // ============================================================
    // الإعدادات
    // ============================================================
    public const string ManageSettingsGlobal = "settings.manage.global";
    public const string ManageSettingsBuilding = "settings.manage.building";
    public const string ManageSettingsOwn = "settings.manage.own";

    // ============================================================
    // أدوات
    // ============================================================
    public const string ManageTools = "tools.manage";
    public const string ManageBackup = "backup.manage";
    public const string ManageSync = "sync.manage";
    public const string SecretAccess = "secret.access";

    // ============================================================
    // كل الصلاحيات
    // ============================================================
    public static readonly List<string> All = new()
    {
        ManageBuildings, ManageAdmins, ManageResidents, DisableApartment,
        ImportTemplate,
        ManageDvrs, ViewCameras, ManageCameras,
        ManageWallet, ManageExpenses, ManageRevenues, ManageCategories, ViewReports,
        ManagePolls, ManageMaintenance, SendNotifications, UseWhatsApp,
        ViewAuditLogAll, ViewAuditLogBuilding, ViewAuditLogOwn,
        ManageSettingsGlobal, ManageSettingsBuilding, ManageSettingsOwn,
        ManageTools, ManageBackup, ManageSync, SecretAccess
    };

    public static readonly List<string> TemplateAdminFull = new()
    {
        ManageResidents, DisableApartment,
        ImportTemplate,
        ManageDvrs, ViewCameras, ManageCameras,
        ManageWallet, ManageExpenses, ManageRevenues,
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

    public static readonly List<string> TemplateAdminSuper = new()
    {
        ManageBuildings, ManageAdmins, ManageResidents, ImportTemplate,
        ManageDvrs, ViewCameras, ManageCameras,
        ManageWallet, ManageExpenses, ManageRevenues,
        ManageCategories, ViewReports,
        ManagePolls, ManageMaintenance, SendNotifications, UseWhatsApp,
        ViewAuditLogAll, ViewAuditLogBuilding, ViewAuditLogOwn,
        ManageSettingsGlobal, ManageSettingsBuilding, ManageSettingsOwn,
        ManageTools, ManageBackup, ManageSync, SecretAccess
    };

    public static readonly List<string> SuperAdminAll = All;

    public static readonly List<string> ResidentBasic = new()
    {
        ManageSettingsOwn, ViewAuditLogOwn
    };

    public static string LabelAr(string key) => key switch
    {
        ManageBuildings => Loc.T("Manage_Buildings"),
        DisableApartment => Loc.T("Disable_Apartment"),
        ManageAdmins => Loc.T("Manage_Admins"),
        ManageResidents => Loc.T("Manage_Residents"),
        ManageDvrs => Loc.T("Manage_DVRs"),
        ViewCameras => Loc.T("View_Cameras"),
        ManageCameras => Loc.T("Manage_Cameras"),
        ImportTemplate => Loc.T("Import_Template"),
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

    public enum PermissionCategory
    {
        Admin,
        Financial,
        Services,
        System
    }

    public static PermissionCategory CategoryOf(string key) => key switch
    {
        ManageBuildings or ManageAdmins or ManageResidents or DisableApartment or ImportTemplate
            => PermissionCategory.Admin,
        ManageDvrs or ViewCameras or ManageCameras
            => PermissionCategory.Services,
        ManageWallet or ManageExpenses or ManageRevenues or ManageCategories or ViewReports
            => PermissionCategory.Financial,
        ManagePolls or ManageMaintenance or SendNotifications or UseWhatsApp
            => PermissionCategory.Services,
        ViewAuditLogAll or ViewAuditLogBuilding or ViewAuditLogOwn
            => PermissionCategory.System,
        ManageSettingsGlobal or ManageSettingsBuilding or ManageSettingsOwn
            => PermissionCategory.System,
        ManageTools or ManageBackup or ManageSync or SecretAccess
            => PermissionCategory.System,
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

    public static string DescriptionAr(string key) => key switch
    {
        ManageBuildings => Loc.T("Create_Edit_Delete_Buildings_Manage_The"),
        ManageAdmins => Loc.T("Add_Edit_Disable_Admins_Manage_Permissions"),
        DisableApartment => Loc.T("Disable_Apartment_Desc"),
        ManageResidents => Loc.T("Add_Edit_Disable_Residents_Reset_PINs"),
        ManageDvrs => Loc.T("Manage_DVRs_Desc"),
        ViewCameras => Loc.T("View_Cameras_Desc"),
        ManageCameras => Loc.T("Manage_Cameras_Desc"),
        ImportTemplate => Loc.T("Import_Template_Desc"),
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

    public static (string Service, string Controller, string Actions) BackendInfo(string key) => key switch
    {
        ManageBuildings => ("BuildingsService", "BuildingsController",
            "Index, Create, Details, QrCodes, QrCodesPdf, Delete, AddFloor, AddApartment"),
        DisableApartment => ("AdminAptsController", "AdminAptsController",
            "DisableApartment, EnableApartment"),
        ManageAdmins => ("AdminManagementService", "AdminsController, PermissionsController",
            "Index, Create, Edit, ChangePin, Deactivate, Activate, Delete"),
        ManageResidents => ("BuildingsService", "AdminAptsController",
            "Index, GenerateQuickQr"),
        ManageDvrs => ("DvrService", "DvrsController",
            "Index, Create, Edit, Delete, Details"),
        ViewCameras => ("CamerasController", "CamerasController",
            "Live, GetHlsUrl"),
        ManageCameras => ("DvrService", "CamerasController",
            "AddCamera, EditCamera, DeleteCamera"),
        ImportTemplate => ("ExcelImportService", "BuildingImportController, BuildingWizardController",
            "Upload, ConfirmImport, Wizard"),
        ManageWallet => ("WalletService", "WalletController",
            "Index, ConfirmDeposit, CancelDeposit, AddAdjustment"),
        ManageExpenses => ("ExpensesService", "ExpensesController", "Index, Add, Delete"),
        ManageRevenues => ("RevenuesService", "RevenuesController", "Index, Add, Delete"),
        ManageCategories => ("CategoriesService", "CategoriesController",
            "Index, Manage, Add, Update, Delete, Reorder"),
        ViewReports => ("ReportsService", "ReportsController", "Index, ExportCsv"),
        ManagePolls => ("PollsService", "PollsController, ResidentPollsController",
            "Index, Create, Close, Reopen, Delete, Vote"),
        ManageMaintenance => ("MaintenanceService", "MaintenanceController, ResidentMaintenanceController",
            "Index, Add, Update, Delete, AddToExpenses"),
        SendNotifications => ("NotificationsService", "NotificationsController",
            "Index, MarkRead, MarkAllRead"),
        UseWhatsApp => ("WhatsAppTemplateService", "WhatsAppController", "Index, Generate"),
        ViewAuditLogAll => ("AuditLogQueryService", "AuditLogController",
            "Index, ExportCsv"),
        ViewAuditLogBuilding => ("AuditLogQueryService", "AuditLogController",
            "Index"),
        ViewAuditLogOwn => ("AuditLogQueryService", "AuditLogController",
            "Index"),
        ManageSettingsGlobal => ("SettingsService", "SettingsController", "Global"),
        ManageSettingsBuilding => ("SettingsService", "SettingsController", "Building"),
        ManageSettingsOwn => ("SettingsService", "SettingsController", "Own"),
        ManageTools => ("MigrationService, DbMaintenanceService", "ToolsController",
            "Index, RunMigration, RunMaintenance, MigrateCarryOver, CleanupData"),
        ManageBackup => ("BackupService, ScheduledBackupService", "BackupController",
            "Index, Create, PreviewRestore, ExecuteRestore, UploadExcel, ConfirmImport, ExportAllJson"),
        ManageSync => ("DbSyncService, ReverseSyncService", "SyncController",
            "Index, SyncAdmins, SyncResidents, SyncPasswords, SyncSql, ReverseSync"),
        SecretAccess => ("ImpersonationService", "ImpersonationController",
            "Enter, Exit, EnterResidentWallet"),
        _ => ("-", "-", "-")
    };
}