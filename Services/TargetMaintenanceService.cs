using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Target-schema maintenance workflow backed exclusively by MaintenanceRecords.
/// </summary>
public sealed class TargetMaintenanceService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetMaintenanceService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<MaintenanceRecord>> GetPageAsync(
        string buildingId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.MaintenanceRecords.AsNoTracking().Where(x => x.BuildingId == buildingId);
        var total = await query.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, pages);
        var entities = await query.OrderByDescending(x => x.MaintenanceDate)
            .ThenByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<MaintenanceRecord>
        {
            Items = entities.Select(Map).ToList(),
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<List<MaintenanceRecord>> GetResidentLogAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var rows = await db.MaintenanceRecords.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.Status != "cancelled")
            .OrderByDescending(x => x.MaintenanceDate).ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<MaintenanceRecord?> GetAsync(
        string buildingId, string id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var row = await db.MaintenanceRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == id, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<MaintenanceRecord> AddAsync(
        string buildingId, string title, string? description, string date, decimal cost,
        string? vendor, string? status, string createdBy, CancellationToken cancellationToken = default)
    {
        var normalizedStatus = NormalizeStatus(status, allowEmpty: true);
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Maintenance title is required.");
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var maintenanceDate))
            throw new ArgumentException("Maintenance date must use yyyy-MM-dd.");

        var row = new MaintenanceRecordEntity
        {
            BuildingId = buildingId,
            Id = "mnt-" + Guid.NewGuid().ToString("N"),
            Title = title.Trim(),
            Description = description?.Trim() ?? "",
            MaintenanceDate = maintenanceDate,
            Cost = cost,
            Vendor = vendor?.Trim() ?? "",
            Status = normalizedStatus,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.MaintenanceRecords.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    public async Task<MaintenanceRecord?> UpdateAsync(
        string buildingId, string id, string title, string? description, string date,
        decimal cost, string? vendor, string? status, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Maintenance title is required.");
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var maintenanceDate))
            throw new ArgumentException("Maintenance date must use yyyy-MM-dd.");
        var normalizedStatus = NormalizeStatus(status, allowEmpty: false);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var row = await db.MaintenanceRecords.FirstOrDefaultAsync(
            x => x.BuildingId == buildingId && x.Id == id, cancellationToken);
        if (row is null) return null;
        if (row.AddedToExpense &&
            (row.Cost != cost ||
             row.MaintenanceDate != maintenanceDate ||
             !string.Equals(row.Title, title.Trim(), StringComparison.Ordinal) ||
             !string.Equals(row.Vendor, vendor?.Trim() ?? "", StringComparison.Ordinal)))
            throw new InvalidOperationException(
                "A maintenance record posted to expenses cannot change its title, date, vendor or cost.");

        row.Title = title.Trim();
        row.Description = description?.Trim() ?? "";
        row.MaintenanceDate = maintenanceDate;
        row.Cost = cost;
        row.Vendor = vendor?.Trim() ?? "";
        row.Status = normalizedStatus;
        await db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    public async Task<bool> DeleteAsync(string buildingId, string id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var row = await db.MaintenanceRecords.FirstOrDefaultAsync(
            x => x.BuildingId == buildingId && x.Id == id, cancellationToken);
        if (row is null) return false;
        if (row.AddedToExpense)
            throw new InvalidOperationException("A maintenance record posted to expenses cannot be deleted.");
        db.MaintenanceRecords.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkAddedToExpenseAsync(
        string buildingId, string id, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var affected = await db.MaintenanceRecords
            .Where(x => x.BuildingId == buildingId && x.Id == id && !x.AddedToExpense)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.AddedToExpense, true)
                .SetProperty(x => x.AddedToExpenseAt, now), cancellationToken);
        return affected == 1 || await db.MaintenanceRecords.AnyAsync(
            x => x.BuildingId == buildingId && x.Id == id && x.AddedToExpense, cancellationToken);
    }

    public async Task<string?> GetBuildingNameAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Buildings.AsNoTracking().Where(x => x.Id == buildingId)
            .Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<string>> GetActiveApartmentIdsAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Apartments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && !x.IsClosed && !x.IsDisabled)
            .OrderBy(x => x.Number).Select(x => x.Id).ToListAsync(cancellationToken);
    }

    private static string NormalizeStatus(string? status, bool allowEmpty)
    {
        var value = string.IsNullOrWhiteSpace(status) && allowEmpty ? "open" : (status ?? "").Trim().ToLowerInvariant();
        if (value is not ("open" or "inprogress" or "done" or "cancelled"))
            throw new ArgumentException("Maintenance status is invalid.");
        return value;
    }

    private static MaintenanceRecord Map(MaintenanceRecordEntity row) => new()
    {
        Id = row.Id,
        Title = row.Title,
        Description = row.Description,
        Date = row.MaintenanceDate.ToString("yyyy-MM-dd"),
        Cost = row.Cost,
        Vendor = row.Vendor,
        Status = row.Status,
        CreatedBy = row.CreatedBy,
        CreatedAt = row.CreatedAt.ToString("O"),
        AddedToExpense = row.AddedToExpense,
        AddedToExpenseAt = row.AddedToExpenseAt?.ToString("O")
    };
}
