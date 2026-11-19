using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Target-schema building and apartment lookups for QR issuance and login.</summary>
public sealed class TargetQrService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetQrService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<QrBuildingPageModel?> GetBuildingPageAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var building = await db.Buildings.AsNoTracking()
            .Where(x => x.Id == buildingId)
            .Select(x => new { x.Name, x.BuildingNumber })
            .FirstOrDefaultAsync(cancellationToken);
        if (building is null) return null;

        var apartments = await db.Apartments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && !x.IsClosed)
            .OrderBy(x => x.Floor!.SortOrder).ThenBy(x => x.Number)
            .Select(x => new QrApartmentInfo
            {
                BuildingId = buildingId,
                BuildingName = building.Name,
                BuildingNumber = building.BuildingNumber,
                ApartmentId = x.Id,
                ApartmentNumber = x.Number,
                Owner = x.Owner,
                Phone = x.Phone,
                FloorLabel = x.Floor == null ? "" : x.Floor.Label,
                FloorOrder = x.Floor == null ? 0 : x.Floor.SortOrder
            })
            .ToListAsync(cancellationToken);

        return new QrBuildingPageModel
        {
            BuildingName = building.Name,
            BuildingNumber = building.BuildingNumber,
            Apartments = apartments
        };
    }

    public async Task<QrApartmentInfo?> GetApartmentAsync(
        string buildingId, string apartmentId, bool requireActive = true,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.Apartments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.Id == apartmentId);
        if (requireActive) query = query.Where(x => !x.IsClosed && !x.IsDisabled);
        return await query.Select(x => new QrApartmentInfo
        {
            BuildingId = x.BuildingId,
            BuildingName = x.Building == null ? "" : x.Building.Name,
            BuildingNumber = x.Building == null ? "" : x.Building.BuildingNumber,
            ApartmentId = x.Id,
            ApartmentNumber = x.Number,
            Owner = x.Owner,
            Phone = x.Phone,
            FloorLabel = x.Floor == null ? "" : x.Floor.Label,
            FloorOrder = x.Floor == null ? 0 : x.Floor.SortOrder
        }).FirstOrDefaultAsync(cancellationToken);
    }
}
