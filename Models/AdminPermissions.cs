namespace BuildingManagementMvc.Models;

/// <summary>
/// Catalog of admin permissions: permission keys, categories, labels and the ready-made templates.
/// </summary>
public static class AdminPermissions
{
    // ============================================================
    // إدارة
    // ============================================================
    /// <summary>
    /// The manage buildings constant.
    /// </summary>
    public const string ManageBuildings = "buildings.manage";
    /// <summary>
    /// The manage admins constant.
    /// </summary>
    public const string ManageAdmins = "admins.manage";
    /// <summary>
    /// The manage residents constant.
    /// </summary>
    public const string ManageResidents = "residents.manage";
    /// <summary>
    /// The disable apartment constant.
    /// </summary>
    public const string DisableApartment = "apartment.disable";

    // ============================================================
    // الكاميرات و DVR ✅ جديد
    // ============================================================
    /// <summary>
    /// The manage dvrs constant.
    /// </summary>
    public const string ManageDvrs = "dvrs.manage";
    /// <summary>
    /// The view cameras constant.
    /// </summary>
    public const string ViewCameras = "cameras.view";
    /// <summary>
    /// The manage cameras constant.
    /// </summary>
    public const string ManageCameras = "cameras.manage";

    // ============================================================
    // استيراد ✅ جديد
    // ============================================================
    /// <summary>
    /// The import template constant.
    /// </summary>
    public const string ImportTemplate = "template.import";

    // ============================================================
    // مالية
    // ============================================================
    /// <summary>
    /// The manage wallet constant.
    /// </summary>
    public const string ManageWallet = "wallet.manage";
    /// <summary>
    /// The manage expenses constant.
    /// </summary>
    public const string ManageExpenses = "expenses.manage";
    /// <summary>
    /// The manage revenues constant.
    /// </summary>
    public const string ManageRevenues = "revenues.manage";
    /// <summary>
    /// The manage categories constant.
    /// </summary>
    public const string ManageCategories = "categories.manage";
    /// <summary>
    /// The view reports constant.
    /// </summary>
    public const string ViewReports = "reports.view";

    // ============================================================
    // خدمات
    // ============================================================
    /// <summary>
    /// The manage polls constant.
    /// </summary>
    public const string ManagePolls = "polls.manage";
    /// <summary>
    /// The manage maintenance constant.
    /// </summary>
    public const string ManageMaintenance = "maintenance.manage";
    /// <summary>
    /// The send notifications constant.
    /// </summary>
    public const string SendNotifications = "notifications.send";
    /// <summary>
    /// The use whats app constant.
    /// </summary>
    public const string UseWhatsApp = "whatsapp.use";

    // ============================================================
    // السجل
    // ============================================================
    /// <summary>
    /// The view audit log all constant.
    /// </summary>
    public const string ViewAuditLogAll = "audit.view.all";
    /// <summary>
    /// The view audit log building constant.
    /// </summary>
    public const string ViewAuditLogBuilding = "audit.view.building";
    /// <summary>
    /// The view audit log own constant.
    /// </summary>
    public const string ViewAuditLogOwn = "audit.view.own";
    /// <summary>Permission to view sanitized application log events.</summary>
    public const string ViewApplicationLogs = "system.logs.view";
    /// <summary>Permission to view system health and readiness diagnostics.</summary>
    public const string ViewSystemHealth = "system.health.view";

    // ============================================================
    // الإعدادات
    // ============================================================
    /// <summary>
    /// The manage settings global constant.
    /// </summary>
    public const string ManageSettingsGlobal = "settings.manage.global";
    /// <summary>
    /// The manage settings building constant.
    /// </summary>
    public const string ManageSettingsBuilding = "settings.manage.building";
    /// <summary>
    /// The manage settings own constant.
    /// </summary>
    public const string ManageSettingsOwn = "settings.manage.own";

    // ============================================================
    // أدوات
    // ============================================================
    /// <summary>
    /// The manage tools constant.
    /// </summary>
    public const string ManageTools = "tools.manage";
    /// <summary>
    /// The manage backup constant.
    /// </summary>
    public const string ManageBackup = "backup.manage";
    /// <summary>Permission to view backup inventory and history.</summary>
    public const string ViewBackups = "backup.view";
    /// <summary>Permission to create and schedule backups.</summary>
    public const string CreateBackup = "backup.create";
    /// <summary>Permission to download backup artifacts and exports.</summary>
    public const string DownloadBackup = "backup.download";
    /// <summary>Permission to preview and execute database restores.</summary>
    public const string RestoreBackup = "backup.restore";
    /// <summary>Permission to delete backup artifacts.</summary>
    public const string DeleteBackup = "backup.delete";
    /// <summary>
    /// The manage sync constant.
    /// </summary>
    public const string ManageSync = "sync.manage";
    /// <summary>
    /// The secret access constant.
    /// </summary>
    public const string SecretAccess = "secret.access";

    // ============================================================
    // كل الصلاحيات
    // ============================================================
    /// <summary>
    /// The all field.
    /// </summary>
    public static readonly List<string> All = new()
    {
        ManageBuildings, ManageAdmins, ManageResidents, DisableApartment,
        ImportTemplate,
        ManageDvrs, ViewCameras, ManageCameras,
        ManageWallet, ManageExpenses, ManageRevenues, ManageCategories, ViewReports,
        ManagePolls, ManageMaintenance, SendNotifications, UseWhatsApp,
        ViewAuditLogAll, ViewAuditLogBuilding, ViewAuditLogOwn, ViewApplicationLogs, ViewSystemHealth,
        ManageSettingsGlobal, ManageSettingsBuilding, ManageSettingsOwn,
        ManageTools, ViewSystemHealth, ManageSync, SecretAccess
    };

    /// <summary>
    /// The template admin full field.
    /// </summary>
    public static readonly List<string> TemplateAdminFull = new()
    {
    ManageResidents, DisableApartment,
    ImportTemplate,
    ManageDvrs, ViewCameras, ManageCameras,
    ManageWallet, ManageExpenses, ManageRevenues,
    ManageCategories, ViewReports,
    ManagePolls, ManageMaintenance,
    SendNotifications, UseWhatsApp,
    ViewAuditLogBuilding, ManageSettingsBuilding
    };
    // ← 17 عنصر

    /// <summary>
    /// The template admin financial field.
    /// </summary>
    public static readonly List<string> TemplateAdminFinancial = new()
    {
        ManageWallet, ManageExpenses, ManageRevenues,
        ManageCategories, ViewReports,
        ViewAuditLogBuilding, ManageSettingsBuilding
    };

    /// <summary>
    /// The template admin maintenance field.
    /// </summary>
    public static readonly List<string> TemplateAdminMaintenance = new()
    {
        ManageMaintenance, SendNotifications,
        ViewAuditLogBuilding, ManageSettingsBuilding
    };

    /// <summary>
    /// The template admin super field.
    /// </summary>
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
    // ← 27 عنصر

    /// <summary>
    /// The super admin all field.
    /// </summary>
    public static readonly List<string> SuperAdminAll = All;

    /// <summary>
    /// The resident basic field.
    /// </summary>
    public static readonly List<string> ResidentBasic = new()
    {
        ManageSettingsOwn, ViewAuditLogOwn
    };

    /// <summary>
    /// Returns the label ar.
    /// </summary>
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
        ViewApplicationLogs => Loc.T("View_Application_Logs"),
        ViewSystemHealth => "View System Health",
        ViewBackups => "View Backup Inventory",
        CreateBackup => "Create / Schedule Backup",
        DownloadBackup => "Download Backup",
        RestoreBackup => "Restore Backup",
        DeleteBackup => "Delete Backup",
        ManageSettingsGlobal => Loc.T("Global_Settings"),
        ManageSettingsBuilding => Loc.T("Building_Settings"),
        ManageSettingsOwn => Loc.T("My_Settings"),
        ManageTools => Loc.T("Advanced_Tools"),
        ManageBackup => Loc.T("Backup"),
        ManageSync => Loc.T("Database_Sync"),
        SecretAccess => Loc.T("Secret_Access"),
        _ => key
    };

    /// <summary>
    /// A group of related admin permissions shown together in the permission matrix.
    /// </summary>
    public enum PermissionCategory
    {
        Admin,
        Financial,
        Services,
        System
    }

    /// <summary>
    /// Returns the category of.
    /// </summary>
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
        ViewAuditLogAll or ViewAuditLogBuilding or ViewAuditLogOwn or ViewApplicationLogs or ViewSystemHealth
            or ViewBackups or CreateBackup or DownloadBackup or RestoreBackup or DeleteBackup
            => PermissionCategory.System,
        ManageSettingsGlobal or ManageSettingsBuilding or ManageSettingsOwn
            => PermissionCategory.System,
        ManageTools or ViewSystemHealth or ManageSync or SecretAccess
            => PermissionCategory.System,
        _ => PermissionCategory.System
    };

    /// <summary>
    /// Returns the category label ar.
    /// </summary>
    public static string CategoryLabelAr(PermissionCategory cat) => cat switch
    {
        PermissionCategory.Admin => Loc.T("Management"),
        PermissionCategory.Financial => Loc.T("Finance_2"),
        PermissionCategory.Services => Loc.T("Services"),
        PermissionCategory.System => Loc.T("System"),
        _ => cat.ToString()
    };

    /// <summary>
    /// Returns the description ar.
    /// </summary>
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
        ViewApplicationLogs => Loc.T("View_Application_Logs_Desc"),
        ViewSystemHealth => "View system health checks, process metrics, and readiness diagnostics.",
        ViewBackups => "View backup inventory, scheduled-backup status, and backup history.",
        CreateBackup => "Create on-demand backups and manage scheduled backup execution.",
        DownloadBackup => "Download protected backup artifacts and data exports.",
        RestoreBackup => "Preview and execute a restore from a validated backup archive.",
        DeleteBackup => "Delete a stored backup artifact.",
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

    /// <summary>
    /// Returns the backend information.
    /// </summary>
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
        ViewBackups or CreateBackup or DownloadBackup or RestoreBackup or DeleteBackup
            => ("BackupService, ScheduledBackupService", "BackupController",
                "Index, Create, PreviewRestore, ExecuteRestore, SaveScheduledSettings, RunNow, DownloadBackup, DeleteBackup"),
        ManageSync => ("DbSyncService, ReverseSyncService", "SyncController",
            "Index, SyncAdmins, SyncResidents, SyncPasswords, SyncSql, ReverseSync"),
        SecretAccess => ("ImpersonationService", "ImpersonationController",
            "Enter, Exit, EnterResidentWallet"),
        _ => ("-", "-", "-")
    };
}