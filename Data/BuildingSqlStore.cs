// =====================================================================
//  BuildingSqlStore - بديل Firestore لعمليات الـ aggregate بتاعة BuildingsService.
//  Singleton (لأن BuildingsService نفسه Singleton) فبيستخدم IDbContextFactory:
//  كل عملية بتفتح DbContext جديد وتقفله.
//
//  Last-write-wins زي Firestore Overwrite الحالي (مفيش optimistic concurrency).
// =====================================================================
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BuildingManagementMvc.Data;

public sealed class BuildingSqlStore
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<BuildingSqlStore> _log;

    public BuildingSqlStore(IDbContextFactory<AppDbContext> factory, ILogger<BuildingSqlStore> log)
    {
        _factory = factory;
        _log = log;
    }

    public async Task<Building?> GetByIdAsync(string id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await BuildingSqlMapper.LoadAsync(db, id);
    }

    // تحذير أداء: 16 استعلام لكل عمارة. للقوائم (اختيار العمارة) الأفضل بعدين
    // استعلام خفيف على Buildings بس.
    public async Task<List<Building>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var ids = await db.Buildings.AsNoTracking()
            .OrderBy(b => b.BuildingNumber).Select(b => b.Id).ToListAsync();
        var list = new List<Building>(ids.Count);
        foreach (var id in ids)
        {
            var b = await BuildingSqlMapper.LoadAsync(db, id);
            if (b != null) list.Add(b);
        }
        return list;
    }

    public async Task SaveFullAsync(Building building)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var warnings = await BuildingSqlMapper.SaveAsync(db, building);
        foreach (var w in warnings)
            _log.LogWarning("Building {BuildingId}: {Warning}", building.Id, w);
    }

    public async Task DeleteAsync(string id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.Buildings.Where(x => x.Id == id).ExecuteDeleteAsync();
    }

    // =====================================================================
    //  SaveSettingsAsync - نسخ إعدادات المبنى على SQL
    //  (بتستخدمها SettingsService.SaveBuildingAsync لما يكون SQL مفعّل)
    // =====================================================================
    public async Task SaveSettingsAsync(string buildingId, Dictionary<string, object> settings)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var entity = await db.BuildingSettings.FirstOrDefaultAsync(x => x.BuildingId == buildingId);
        if (entity == null)
        {
            entity = new BuildingSettingsEntity { BuildingId = buildingId };
            db.BuildingSettings.Add(entity);
        }

        entity.DisplayName = GetString(settings, "displayName");
        entity.Address = GetString(settings, "address");
        entity.WhatsappNumber = GetString(settings, "whatsappNumber");
        entity.InvoiceDayOfMonth = GetInt(settings, "invoiceDayOfMonth", 1);
        entity.ExpenseDistribution = GetString(settings, "expenseDistribution", "equal");
        entity.VotingQuorumPercent = GetInt(settings, "votingQuorumPercent", 50);
        entity.Currency = GetString(settings, "currency", "EGP");
        entity.UpdatedAt = ParseTs(settings, "updatedAt");
        entity.UpdatedBy = GetString(settings, "updatedBy");

        await db.SaveChangesAsync();
        SqlMirrorHealth.RecordSuccess();
    }

    // ---------------------------------------------------------------------
    //  Helpers للـ Dictionary
    // ---------------------------------------------------------------------
    private static string GetString(Dictionary<string, object> d, string key, string def = "")
        => d.TryGetValue(key, out var v) && v != null ? v.ToString() ?? def : def;

    private static int GetInt(Dictionary<string, object> d, string key, int def)
        => d.TryGetValue(key, out var v) && v != null && int.TryParse(v.ToString(), out var i) ? i : def;

    private static DateTime? ParseTs(Dictionary<string, object> d, string key)
    {
        if (!d.TryGetValue(key, out var v) || v == null) return null;
        if (DateTime.TryParse(v.ToString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            return new DateTime(dt.Ticks - dt.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        return null;
    }
}