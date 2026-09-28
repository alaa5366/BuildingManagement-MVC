using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Services;

public class DbMaintenanceService
{
    private readonly FirestoreDb _db;
    private readonly UsersService _users;
    private readonly BuildingsService _buildings;
    private readonly WalletService _wallet;
    private readonly IAuditLogger _audit;

    public DbMaintenanceService(
        FirestoreContext ctx,
        UsersService users,
        BuildingsService buildings,
        WalletService wallet,
        IAuditLogger audit)
    {
        _db = ctx.Db;
        _users = users;
        _buildings = buildings;
        _wallet = wallet;
        _audit = audit;
    }

    // ============================================================
    // 1. كشف التكرار في users
    // ============================================================
    public async Task<MaintenanceResult> FindDuplicateUsersAsync(bool fix)
    {
        var result = new MaintenanceResult { ToolName = "كشف تكرار المستخدمين" };
        var snapshot = await _db.Collection("users").GetSnapshotAsync();

        var allUsers = snapshot.Documents.Select(d => new
        {
            Doc = d,
            Data = d.ToDictionary()
        }).ToList();

        // تجميع حسب phone + buildingIds
        var grouped = allUsers
            .Where(u => u.Data.ContainsKey("phone") && u.Data["phone"] != null)
            .GroupBy(u => u.Data["phone"].ToString())
            .Where(g => g.Count() > 1)
            .ToList();

        result.ItemsFound = grouped.Count;

        foreach (var group in grouped)
        {
            result.Details.Add($"📱 phone: {group.Key} → {group.Count()} مستند");
            foreach (var item in group)
            {
                result.Details.Add($"   - {item.Doc.Id}");
            }

            if (fix)
            {
                // نمسك الأحدث ونمسح الباقي
                var docs = group.OrderByDescending(g =>
                {
                    g.Data.TryGetValue("createdAt", out var c);
                    return c?.ToString() ?? "";
                }).ToList();

                for (int i = 1; i < docs.Count; i++)
                {
                    await docs[i].Doc.Reference.DeleteAsync();
                    result.ItemsFixed++;
                }
            }
        }

        return result;
    }

    // ============================================================
    // 2. كشف دفعات معلقة قديمة
    // ============================================================
    public async Task<MaintenanceResult> FindStalePendingDepositsAsync(int olderThanDays, bool cancel)
    {
        var result = new MaintenanceResult { ToolName = $"دفعات معلقة > {olderThanDays} يوم" };
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);

        var buildings = await _buildings.GetAllAsync();

        foreach (var building in buildings)
        {
            foreach (var monthKv in building.Months)
            {
                foreach (var deposit in monthKv.Value.Deposits.Where(d => d.Status == "pending"))
                {
                    if (DateTime.TryParse(deposit.CreatedAt, out var created) && created < cutoff)
                    {
                        result.ItemsFound++;
                        result.Details.Add($"💰 {building.Name} — دفعة {deposit.Number} ({deposit.Amount:0.##} ج.م)");

                        if (cancel)
                        {
                            deposit.Status = "cancelled";
                            deposit.CancelledAt = DateTime.UtcNow.ToString("o");
                            deposit.CancelledBy = "system-maintenance";
                            deposit.CancelledReason = $"إلغاء تلقائي — أقدم من {olderThanDays} يوم";
                            result.ItemsFixed++;
                        }
                    }
                }
            }

            if (cancel && result.ItemsFixed > 0)
                await _buildings.SaveFullAsync(building);
        }

        return result;
    }

    // ============================================================
    // 3. إعادة حساب أرصدة الشقق
    // ============================================================
    public async Task<MaintenanceResult> RecalculateBalancesAsync(string buildingId)
    {
        var result = new MaintenanceResult { ToolName = "إعادة حساب الأرصدة" };

        var building = await _buildings.GetByIdAsync(buildingId);
        if (building == null) return result;

        var currentMonth = WalletService.CurrentMonthKey();

        foreach (var monthKv in building.Months)
        {
            var mk = monthKv.Key;
            var m = monthKv.Value;

            // إعادة حساب توزيع المصروفات
            var newDist = _wallet.RecalculateDistribution(building, mk);
            m.Distribution = newDist;

            // إعادة حساب توزيع الإيرادات
            var newRevDist = _wallet.RecalculateRevenueDistribution(building, mk);
            m.RevenueDistribution = newRevDist;

            result.ItemsFixed++;
            result.Details.Add($"📅 {mk}: توزيع جديد = {newDist.Count} شقة");
        }

        await _buildings.SaveFullAsync(building);
        return result;
    }

    // ============================================================
    // 4. كشف اليتامى (دفعات مرتبطة بشقق مش موجودة)
    // ============================================================
    public async Task<MaintenanceResult> FindOrphanedDepositsAsync(bool fix)
    {
        var result = new MaintenanceResult { ToolName = "كشف المراجع اليتيمة" };
        var buildings = await _buildings.GetAllAsync();

        foreach (var building in buildings)
        {
            var validAptIds = building.Apartments.Select(a => a.Id).ToHashSet();

            foreach (var monthKv in building.Months)
            {
                foreach (var deposit in monthKv.Value.Deposits)
                {
                    if (!validAptIds.Contains(deposit.AptId))
                    {
                        result.ItemsFound++;
                        result.Details.Add($"⚠️ {building.Name} — دفعة {deposit.Number} مرتبطة بشقة مش موجودة ({deposit.AptId})");

                        if (fix)
                        {
                            deposit.Status = "cancelled";
                            deposit.CancelledReason = "شقة مش موجودة — إلغاء تلقائي";
                            result.ItemsFixed++;
                        }
                    }
                }
            }

            if (fix && result.ItemsFixed > 0)
                await _buildings.SaveFullAsync(building);
        }

        return result;
    }
}