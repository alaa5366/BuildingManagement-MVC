using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Target-schema building and apartment lookups for QR issuance and login.</summary>
public sealed record TargetResidentAccount(Guid AccountId, string Name, string Phone, bool IsPrimary);

    /// <summary> Represents the TargetQrService data or service contract. </summary>
public sealed class TargetQrService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetQrService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>Gets QR access records for the specified building page.</summary>
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
            .Where(x => x.BuildingId == buildingId && !x.IsClosed && !x.IsDisabled)
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

    /// <summary>Gets apartment details required for QR access processing.</summary>
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

    /// <summary>Lists active target-identity resident accounts for an apartment.</summary>
    public async Task<List<TargetResidentAccount>> GetResidentAccountsForApartmentAsync(
        string buildingId, string apartmentId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var rows = await (
            from occupancy in db.ApartmentOccupancies.AsNoTracking()
            join person in db.Persons.AsNoTracking() on occupancy.PersonId equals person.PersonId
            join account in db.UserAccounts.AsNoTracking() on person.PersonId equals account.PersonId
            where occupancy.BuildingId == buildingId && occupancy.ApartmentId == apartmentId
                && occupancy.ValidFromUtc <= now && (occupancy.ValidToUtc == null || occupancy.ValidToUtc > now)
                && !person.IsArchived && account.AccountStatus == 1
                && db.UserRoleAssignments.Any(assignment => assignment.AccountId == account.AccountId
                    && assignment.RoleKey == "resident" && assignment.RevokedAtUtc == null
                    && assignment.ValidFromUtc <= now
                    && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                    && ((assignment.ScopeType == 1 && assignment.ScopeBuildingId == buildingId)
                        || (assignment.ScopeType == 2 && assignment.ScopeBuildingId == buildingId
                            && assignment.ScopeApartmentId == apartmentId)))
            orderby occupancy.IsPrimary descending, person.DisplayName
            select new { account.AccountId, person.DisplayName, Phone = person.PrimaryPhone ?? "", occupancy.IsPrimary })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(item => item.AccountId)
            .Select(group => group.First())
            .Select(item => new TargetResidentAccount(item.AccountId, item.DisplayName, item.Phone, item.IsPrimary))
            .ToList();
    }
}
