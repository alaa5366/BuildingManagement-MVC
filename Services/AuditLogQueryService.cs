using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

public class AuditLogQueryService
{
    private readonly IAuditLogger _audit;

    public AuditLogQueryService(IAuditLogger audit)
    {
        _audit = audit;
    }

    public Task<List<AuditEntryDoc>> QueryAsync(
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        string? action = null,
        string? severity = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 500)
    {
        return _audit.QueryAsync(buildingId, apartmentId, userId, from, to, action, severity, limit);
    }

    // ============================================================
    // Known Actions
    // ============================================================
    public static readonly List<string> KnownActions = new()
    {
        // Admin
        "admin.create", "admin.update", "admin.change_pin", "admin.deactivate",
        "admin.activate", "admin.delete", "admin.sync_permissions",

        // Categories
        "category.expense.add", "category.expense.update", "category.expense.delete",
        "category.expense.deactivate", "category.expense.reorder",
        "category.revenue.add", "category.revenue.update", "category.revenue.delete",
        "category.revenue.deactivate", "category.revenue.reorder",

        // Deposits
        "deposit_create", "deposit_confirm", "deposit_cancel", "deposit_update",

        // Expenses / Revenues
        "expense_add", "expense_delete", "revenue_add", "revenue_delete",

        // Wallet
        "wallet_adjustment", "report_export", "invoice_download",

        // Maintenance
        "maint_add", "maint_update", "maint_delete", "maint_add_expense",

        // Polls
        "poll_create", "poll_close", "poll_reopen", "poll_delete", "poll_vote",

        // QR
        "qr_access_generate",

        // Residents
        "resident.create", "resident.update", "resident.profile_update",

        // Buildings
        "building.create", "building.delete",

        // Settings
        "settings.own.update", "settings.building.update", "settings.global.update",
        "superadmin.name_update", "admin.profile_update",

        // Apartments
        "apartment.opened",
        "apartment.closed",

        // Impersonation
        "impersonation.enter",
        "impersonation.exit"
    };

    public static string LabelAr(string action) => action switch
    {
        "admin.create" => Loc.T("Create_Admin"),
        "admin.update" => Loc.T("Edit_Admin"),
        "admin.change_pin" => Loc.T("Change_Admin_PIN"),
        "admin.deactivate" => Loc.T("Disable_Admin"),
        "admin.activate" => Loc.T("Activate_Admin"),
        "admin.delete" => Loc.T("Delete_Admin"),
        "admin.sync_permissions" => Loc.T("Sync_Permissions"),
        "admin.profile_update" => Loc.T("Update_Admin_Details"),
        "category.expense.add" => Loc.T("Add_Expense_Category_2"),
        "category.expense.update" => Loc.T("Edit_Expense_Category"),
        "category.expense.delete" => Loc.T("Delete_Expense_Category"),
        "category.expense.deactivate" => Loc.T("Disable_Expense_Category"),
        "category.expense.reorder" => Loc.T("Reorder_Expense_Categories"),
        "category.revenue.add" => Loc.T("Add_Revenue_Category_2"),
        "category.revenue.update" => Loc.T("Edit_Revenue_Category"),
        "category.revenue.delete" => Loc.T("Delete_Revenue_Category"),
        "category.revenue.deactivate" => Loc.T("Disable_Revenue_Category"),
        "category.revenue.reorder" => Loc.T("Reorder_Revenue_Categories"),
        "deposit_create" => Loc.T("Create_Payment"),
        "deposit_confirm" => Loc.T("Confirm_Payment"),
        "deposit_cancel" => Loc.T("Cancel_Payment"),
        "deposit_update" => Loc.T("Edit_Payment_2"),
        "expense_add" => Loc.T("Add_Expense_2"),
        "expense_delete" => Loc.T("Delete_Expense"),
        "revenue_add" => Loc.T("Add_Revenue_2"),
        "revenue_delete" => Loc.T("Delete_Revenue"),
        "wallet_adjustment" => Loc.T("Wallet_Adjustment"),
        "report_export" => Loc.T("Export_Report"),
        "invoice_download" => Loc.T("Download_Invoice"),
        "maint_add" => Loc.T("Add_Maintenance"),
        "maint_update" => Loc.T("Edit_Maintenance"),
        "maint_delete" => Loc.T("Delete_Maintenance"),
        "maint_add_expense" => Loc.T("Add_Maintenance_Cost_As_Expense"),
        "poll_create" => Loc.T("Create_Poll"),
        "poll_close" => Loc.T("Close_Poll"),
        "poll_reopen" => Loc.T("Reopen_Poll"),
        "poll_delete" => Loc.T("Delete_Poll"),
        "poll_vote" => Loc.T("Vote"),
        "qr_access_generate" => Loc.T("Generate_QR_Login_Link"),
        "resident.create" => Loc.T("Add_Resident"),
        "resident.update" => Loc.T("Edit_Resident"),
        "resident.profile_update" => Loc.T("Update_Resident_Details"),
        "building.create" => Loc.T("Create_Building_2"),
        "building.delete" => Loc.T("Delete_Building_2"),
        "settings.own.update" => Loc.T("Update_My_Settings"),
        "settings.building.update" => Loc.T("Update_Building_Settings"),
        "settings.global.update" => Loc.T("Update_Global_Settings"),
        "superadmin.name_update" => Loc.T("Update_Super_Admin_Name"),
        "apartment.opened" => Loc.T("Open_Apartment_2"),
        "apartment.closed" => Loc.T("Close_Apartment_2"),
        "impersonation.enter" => Loc.T("Log_In_As_Resident_Impersonation"),
        "impersonation.exit" => Loc.T("Return_From_Impersonation"),
        _ => action
    };

    public static string SeverityBadgeClass(string severity) => severity switch
    {
        "critical" => "bg-danger",
        "warning" => "bg-warning text-dark",
        "info" => "bg-info text-dark",
        _ => "bg-secondary"
    };

    public static string SeverityLabelAr(string severity) => severity switch
    {
        "critical" => Loc.T("Critical"),
        "warning" => Loc.T("Warning"),
        "info" => Loc.T("Info"),
        _ => severity
    };
}