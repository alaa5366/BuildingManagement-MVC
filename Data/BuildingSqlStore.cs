// =====================================================================
//  BuildingSqlStore - بديل Firestore لعمليات الـ aggregate بتاعة BuildingsService.
//  Singleton (لأن BuildingsService نفسه Singleton) فبيستخدم IDbContextFactory:
//  كل عملية بتفتح DbContext جديد وتقفله.
//
//  Optimistic concurrency is enforced by Buildings.RowVersion for every aggregate save.
// =====================================================================
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Loads and saves buildings (the whole aggregate) from SQL Server through EF Core.
/// </summary>
public class BuildingSqlStore
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<BuildingSqlStore> _log;

    /// <summary>
    /// Initializes a new instance of the <see cref="BuildingSqlStore"/> class.
    /// </summary>
    public BuildingSqlStore(IDbContextFactory<AppDbContext> factory, ILogger<BuildingSqlStore> log)
    {
        _factory = factory;
        _log = log;
    }

    /// <summary>
    /// Gets the by ID.
    /// </summary>
    public virtual async Task<Building?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await BuildingSqlMapper.LoadAsync(db, id, ct);
    }

    // تحذير أداء: 16 استعلام لكل عمارة. للقوائم (اختيار العمارة) الأفضل بعدين
    // استعلام خفيف على Buildings بس.
    /// <summary>
    /// Gets the all.
    /// </summary>
    public async Task<List<Building>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var ids = await db.Buildings.AsNoTracking()
            .OrderBy(b => b.BuildingNumber).Select(b => b.Id).ToListAsync(ct);
        var list = new List<Building>(ids.Count);
        foreach (var id in ids)
        {
            var b = await BuildingSqlMapper.LoadAsync(db, id, ct);
            if (b != null) list.Add(b);
        }
        return list;
    }

    // =====================================================================
    //  GetSummariesAsync — خفيف: Id/Name/BuildingNumber فقط
    //  (لصفحات الدخول والقوائم — استعلام واحد بدل 16×N)
    // =====================================================================
    /// <summary>
    /// Gets the summaries.
    /// </summary>
    public virtual async Task<List<Building>> GetSummariesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var rows = await db.Buildings.AsNoTracking()
            .OrderBy(b => b.BuildingNumber)
            .Select(b => new { b.Id, b.Name, b.BuildingNumber })
            .ToListAsync(ct);
        return rows.Select(r => new Building
        {
            Id = r.Id,
            Name = r.Name,
            BuildingNumber = r.BuildingNumber
        }).ToList();
    }
    /// <summary>
    /// يسترجع صفحة من ملخصات المباني من قاعدة البيانات.
    /// </summary>
    /// <param name="page">رقم الصفحة (يبدأ من 1).</param>
    /// <param name="pageSize">عدد العناصر في الصفحة.</param>
    /// <param name="ct">رمز إلغاء العملية.</param>
    /// <returns>صفحة من ملخصات المباني.</returns>
    public virtual async Task<BuildingManagementMvc.Models.PagedResult<Building>> GetSummariesPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var query = db.Buildings.AsNoTracking().OrderBy(b => b.BuildingNumber);
        var total = await query.CountAsync(ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new { b.Id, b.Name, b.BuildingNumber }).ToListAsync(ct);
        return new BuildingManagementMvc.Models.PagedResult<Building>
        {
            Items = rows.Select(x => new Building { Id = x.Id, Name = x.Name, BuildingNumber = x.BuildingNumber }).ToList(),
            CurrentPage = page, PageSize = pageSize, TotalItems = total
        };
    }

    // =====================================================================
    //  GetSummariesWithCountsAsync — نفس GetSummariesAsync
    //  لكن بترجع كمان عدد الشقق والأدوار (بدون تحميلهم فعلياً).
    //  مفيدة للـ Views اللي بتستخدم @b.Apartments.Count و @b.Floors.Count.
    // =====================================================================
    /// <summary>
    /// Gets the summaries with counts.
    /// </summary>
    public virtual async Task<List<Building>> GetSummariesWithCountsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

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
            .ToListAsync(ct);

        return rows.Select(r => new Building
        {
            Id = r.Id,
            Name = r.Name,
            BuildingNumber = r.BuildingNumber,
            ApartmentsCount = r.ApartmentsCount,
            FloorsCount = r.FloorsCount
        }).ToList();
    }

    /// <summary>
    /// Finds the building ID by apartment.
    /// </summary>
    public virtual async Task<string?> FindBuildingIdByApartmentAsync(string apartmentId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Apartments.AsNoTracking()
            .Where(a => a.Id == apartmentId)
            .Select(a => a.BuildingId)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Saves the full.
    /// </summary>
    public async Task SaveFullAsync(Building building, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var warnings = await BuildingSqlMapper.SaveAsync(db, building, ct);
        foreach (var w in warnings)
            _log.LogWarning("Building {BuildingId}: {Warning}", building.Id, w);
    }

    /// <summary>
    /// Deletes the operation on the building SQL.
    /// </summary>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Buildings.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
    }

    // =====================================================================
    //  SaveSettingsAsync - نسخ إعدادات المبنى على SQL
    //  (بتستخدمها SettingsService.SaveBuildingAsync لما يكون SQL مفعّل)
    // =====================================================================
    /// <summary>
    /// Saves the settings.
    /// </summary>
    public async Task SaveSettingsAsync(string buildingId, Dictionary<string, object> settings, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var entity = await db.BuildingSettings.FirstOrDefaultAsync(x => x.BuildingId == buildingId, ct);
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

        await db.SaveChangesAsync(ct);
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