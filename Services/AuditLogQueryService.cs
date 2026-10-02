using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Services;

public class AuditLogQueryService
{
    private readonly FirestoreDb _db;
    private readonly BuildingsService _buildings;
    private const string Collection = "auditLogs";

    public AuditLogQueryService(FirestoreContext ctx, BuildingsService buildings)
    {
        _db = ctx.Db;
        _buildings = buildings;
    }

    // ============================================================
    // استعلام موحّد
    // ============================================================
    public async Task<List<AuditEntryDoc>> QueryAsync(
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        string? action = null,
        string? severity = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 500)
    {
        var results = new List<AuditEntryDoc>();

        var newEntries = await QueryNewAsync(
            buildingId, apartmentId, userId, action, severity, from, to, limit);
        results.AddRange(newEntries);

        Console.WriteLine($"[AuditLogQuery] auditLogs: {newEntries.Count} entries");

        if (!string.IsNullOrWhiteSpace(buildingId))
        {
            var oldEntries = await QueryOldAsync(
                buildingId, apartmentId, userId, action, from, to);
            results.AddRange(oldEntries);
        }

        return results
            .OrderByDescending(e => e.EffectiveDate)
            .Take(limit)
            .ToList();
    }

    // ============================================================
    // استعلام auditLogs
    // ============================================================
    private async Task<List<AuditEntryDoc>> QueryNewAsync(
        string? buildingId, string? apartmentId, string? userId,
        string? action, string? severity,
        DateTime? from, DateTime? to, int limit)
    {
        var list = new List<AuditEntryDoc>();

        try
        {
            var snapshot = await _db.Collection(Collection).GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var data = doc.ToDictionary();

                    var entry = new AuditEntryDoc
                    {
                        Id = doc.Id,
                        Action = GetString(data, "action"),
                        BuildingId = GetStringOrNull(data, "buildingId"),
                        ApartmentId = GetStringOrNull(data, "apartmentId"),
                        UserId = GetStringOrNull(data, "userId"),
                        UserRole = GetStringOrNull(data, "userRole"),
                        Severity = GetString(data, "severity", "info"),
                        Details = GetStringOrNull(data, "details"),
                        CreatedAt = GetTimestamp(data, "createdAt"),
                        Timestamp = GetTimestamp(data, "timestamp"),
                        Metadata = GetMetadata(data)
                    };

                    if (!string.IsNullOrWhiteSpace(buildingId) && entry.BuildingId != buildingId)
                        continue;
                    if (!string.IsNullOrWhiteSpace(apartmentId) && entry.ApartmentId != apartmentId)
                        continue;
                    if (!string.IsNullOrWhiteSpace(userId) && entry.UserId != userId)
                        continue;
                    if (!string.IsNullOrWhiteSpace(action) && entry.Action != action)
                        continue;
                    if (!string.IsNullOrWhiteSpace(severity) && entry.Severity != severity)
                        continue;
                    if (from.HasValue && entry.EffectiveDate < from.Value)
                        continue;
                    if (to.HasValue && entry.EffectiveDate > to.Value)
                        continue;

                    list.Add(entry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AuditLogQuery] Doc {doc.Id} FAILED: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLogQuery] OUTER ERROR: {ex.Message}");
        }

        return list
            .OrderByDescending(e => e.EffectiveDate)
            .Take(limit)
            .ToList();
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static string GetString(Dictionary<string, object> data, string key, string defaultVal = "")
    {
        if (data.TryGetValue(key, out var v) && v != null)
            return v.ToString() ?? defaultVal;
        return defaultVal;
    }

    private static string? GetStringOrNull(Dictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var v) && v != null)
            return v.ToString();
        return null;
    }

    private static Google.Cloud.Firestore.Timestamp? GetTimestamp(
        Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var v) || v == null) return null;
        if (v is Google.Cloud.Firestore.Timestamp ts) return ts;
        if (v is string s && DateTime.TryParse(s, out var dt))
            return Google.Cloud.Firestore.Timestamp.FromDateTime(dt.ToUniversalTime());
        return null;
    }

    private static Dictionary<string, object>? GetMetadata(Dictionary<string, object> data)
    {
        if (!data.TryGetValue("metadata", out var v) || v == null) return null;
        if (v is Dictionary<string, object> dict) return dict;
        if (v is IReadOnlyDictionary<string, object> ro)
            return new Dictionary<string, object>(ro);
        return null;
    }

    // ============================================================
    // استعلام building.AuditLog (القديمة)
    // ============================================================
    private async Task<List<AuditEntryDoc>> QueryOldAsync(
        string buildingId, string? apartmentId, string? userId,
        string? action, DateTime? from, DateTime? to)
    {
        var list = new List<AuditEntryDoc>();

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building?.AuditLog == null) return list;

        foreach (var old in building.AuditLog)
        {
            if (!string.IsNullOrWhiteSpace(action) && old.Action != action) continue;
            if (!string.IsNullOrWhiteSpace(userId) && old.Actor != userId) continue;
            if (!string.IsNullOrWhiteSpace(apartmentId) &&
                old.AptNumber?.ToString() != apartmentId) continue;

            DateTime created = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(old.Ts))
                DateTime.TryParse(old.Ts, out created);

            if (from.HasValue && created < from.Value) continue;
            if (to.HasValue && created > to.Value) continue;

            var effectiveCreated = created == DateTime.MinValue ? DateTime.UtcNow : created;

            if (effectiveCreated.Kind == DateTimeKind.Unspecified)
                effectiveCreated = DateTime.SpecifyKind(effectiveCreated, DateTimeKind.Utc);
            else if (effectiveCreated.Kind == DateTimeKind.Local)
                effectiveCreated = effectiveCreated.ToUniversalTime();

            list.Add(new AuditEntryDoc
            {
                Id = old.Id ?? "",
                Action = old.Action ?? "",
                BuildingId = buildingId,
                ApartmentId = old.AptNumber?.ToString(),
                UserId = old.Actor ?? "",
                UserRole = old.ActorRole ?? "",
                Severity = "info",
                CreatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(effectiveCreated),
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "building.AuditLog",
                    ["details"] = old.Details ?? "",
                    ["label"] = old.Label ?? ""
                }
            });
        }

        return list;
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

        // ✅ Phase 24.4-24.5
        "apartment.opened",
        "apartment.closed",
        "impersonation.enter",
        "impersonation.exit"
    };

    // ============================================================
    // LabelAr
    // ============================================================
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

        // ✅ Phase 24.4-24.5
        "apartment.opened" => Loc.T("Open_Apartment_2"),
        "apartment.closed" => Loc.T("Close_Apartment_2"),
        "impersonation.enter" => Loc.T("Log_In_As_Resident_Impersonation"),
        "impersonation.exit" => Loc.T("Return_From_Impersonation"),

        _ => action
    };

    // ============================================================
    // Severity Labels
    // ============================================================
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