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

public class BuildingSqlStore
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<BuildingSqlStore> _log;

    public BuildingSqlStore(IDbContextFactory<AppDbContext> factory, ILogger<BuildingSqlStore> log)
    {
        _factory = factory;
        _log = log;
    }

    public virtual async Task<Building?> GetByIdAsync(string id)
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

    // =====================================================================
    //  GetSummariesAsync — خفيف: Id/Name/BuildingNumber فقط
    //  (لصفحات الدخول والقوائم — استعلام واحد بدل 16×N)
    // =====================================================================
    public virtual async Task<List<Building>> GetSummariesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var rows = await db.Buildings.AsNoTracking()
            .OrderBy(b => b.BuildingNumber)
            .Select(b => new { b.Id, b.Name, b.BuildingNumber })
            .ToListAsync();
        return rows.Select(r => new Building
        {
            Id = r.Id,
            Name = r.Name,
            BuildingNumber = r.BuildingNumber
        }).ToList();
    }

    // =====================================================================
    //  GetSummariesWithCountsAsync — نفس GetSummariesAsync
    //  لكن بترجع كمان عدد الشقق والأدوار (بدون تحميلهم فعلياً).
    //  مفيدة للـ Views اللي بتستخدم @b.Apartments.Count و @b.Floors.Count.
    // =====================================================================
    public virtual async Task<List<Building>> GetSummariesWithCountsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        // استعلام واحد — بترجع البيانات الأساسية + Counts
        var rows = await db.Buildings.AsNoTracking()
            .OrderBy(b => b.BuildingNumber)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.BuildingNumber,
                ApartmentsCount = b.Apartments.Count(),
                FloorsCount = b.Floors.Count()
            })
            .ToListAsync();

        return rows.Select(r => new Building
        {
            Id = r.Id,
            Name = r.Name,
            BuildingNumber = r.BuildingNumber,
            // pseudo-objects للعرض فقط — Count هيشتغل عادي
            Apartments = Enumerable.Range(0, r.ApartmentsCount)
                .Select(_ => new Apartment()).ToList(),
            Floors = Enumerable.Range(0, r.FloorsCount)
                .Select(_ => new Floor()).ToList()
        }).ToList();
    }

    public virtual async Task<string?> FindBuildingIdByApartmentAsync(string apartmentId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Apartments.AsNoTracking()
            .Where(a => a.Id == apartmentId)
            .Select(a => a.BuildingId)
            .FirstOrDefaultAsync();
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