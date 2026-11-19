using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Maintenance workflows backed by normalized operational requests and immutable finance links.</summary>
public sealed class TargetMaintenanceService
{
    private const byte Open = 0;
    private const byte InProgress = 1;
    private const byte Done = 2;
    private const byte Cancelled = 3;
    private readonly IDbContextFactory<AppDbContext> _factory;

    /// <summary>Creates the target maintenance service.</summary>
    public TargetMaintenanceService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>Gets a paged maintenance list for one building.</summary>
    public async Task<PagedResult<MaintenanceRecord>> GetPageAsync(
        string buildingId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidateBuildingId(buildingId);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.MaintenanceRequests.AsNoTracking().Where(x => x.BuildingId == buildingId);
        var total = await query.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, pages);
        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.RequestId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(x => x.Assignments).Include(x => x.LedgerLinks).ToListAsync(cancellationToken);
        var items = new List<MaintenanceRecord>(rows.Count);
        foreach (var row in rows) items.Add(await MapAsync(db, row, cancellationToken));
        return new PagedResult<MaintenanceRecord>
        {
            Items = items, CurrentPage = page, PageSize = pageSize, TotalItems = total
        };
    }

    /// <summary>Gets building-wide maintenance announcements visible to residents.</summary>
    public async Task<List<MaintenanceRecord>> GetResidentLogAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        ValidateBuildingId(buildingId);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var rows = await db.MaintenanceRequests.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.ApartmentId == null && x.Status != Cancelled)
            .OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.RequestId)
            .Take(500).Include(x => x.Assignments).Include(x => x.LedgerLinks)
            .ToListAsync(cancellationToken);
        var items = new List<MaintenanceRecord>(rows.Count);
        foreach (var row in rows) items.Add(await MapAsync(db, row, cancellationToken));
        return items;
    }

    /// <summary>Gets one maintenance request within its building scope.</summary>
    public async Task<MaintenanceRecord?> GetAsync(
        string buildingId, string id, CancellationToken cancellationToken = default)
    {
        ValidateBuildingId(buildingId);
        var requestId = ParseId(id);
        if (requestId is null) return null;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var row = await db.MaintenanceRequests.AsNoTracking()
            .Include(x => x.Assignments).Include(x => x.LedgerLinks)
            .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.RequestId == requestId, cancellationToken);
        return row is null ? null : await MapAsync(db, row, cancellationToken);
    }

    /// <summary>Creates a maintenance request and records its initial status and vendor assignment.</summary>
    public async Task<MaintenanceRecord> AddAsync(
        string buildingId, string title, string? description, string date, decimal cost,
        string? vendor, string? status, string createdBy, CancellationToken cancellationToken = default)
    {
        ValidateBuildingId(buildingId);
        ValidateText(title, 200, nameof(title), required: true);
        ValidateText(description, 4000, nameof(description));
        ValidateText(vendor, 200, nameof(vendor));
        ValidateText(createdBy, 160, nameof(createdBy));
        ValidateNoUnpostedCost(cost);
        var statusCode = ParseStatus(NormalizeStatus(status, allowEmpty: true));
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var requestDate))
            throw new ArgumentException("Maintenance date must use yyyy-MM-dd.", nameof(date));

        var now = DateTime.UtcNow;
        var requestId = Guid.NewGuid();
        var row = new MaintenanceRequestEntity
        {
            RequestId = requestId,
            BuildingId = buildingId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Status = statusCode,
            Priority = 2,
            RequestedByReference = createdBy.Trim(),
            CreatedAtUtc = new DateTime(requestDate.Year, requestDate.Month, requestDate.Day, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = now,
            ClosedAtUtc = statusCode == Done || statusCode == Cancelled ? now : null
        };
        row.StatusEvents.Add(new MaintenanceStatusEventEntity
        {
            StatusEventId = Guid.NewGuid(), RequestId = requestId, FromStatus = null,
            ToStatus = statusCode, ActorReference = createdBy.Trim(),
            Reason = "Request created", OccurredAtUtc = now
        });
        if (!string.IsNullOrWhiteSpace(vendor))
            row.Assignments.Add(new MaintenanceAssignmentEntity
            {
                AssignmentId = Guid.NewGuid(), RequestId = requestId,
                AssigneeReference = createdBy.Trim(), VendorName = vendor.Trim(),
                AssignedAtUtc = now
            });

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.MaintenanceRequests.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return await MapAsync(db, row, cancellationToken);
    }

    /// <summary>Updates request metadata while preserving posted financial source records.</summary>
    public async Task<MaintenanceRecord?> UpdateAsync(
        string buildingId, string id, string title, string? description, string date,
        decimal cost, string? vendor, string? status, CancellationToken cancellationToken = default)
    {
        ValidateBuildingId(buildingId);
        ValidateText(title, 200, nameof(title), required: true);
        ValidateText(description, 4000, nameof(description));
        ValidateText(vendor, 200, nameof(vendor));
        ValidateNoUnpostedCost(cost);
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var requestDate))
            throw new ArgumentException("Maintenance date must use yyyy-MM-dd.", nameof(date));
        var newStatus = ParseStatus(NormalizeStatus(status, allowEmpty: false));
        var requestId = ParseId(id);
        if (requestId is null) return null;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var row = await db.MaintenanceRequests.Include(x => x.Assignments).Include(x => x.LedgerLinks)
            .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.RequestId == requestId, cancellationToken);
        if (row is null) return null;
        if (await HasPostedLedgerLinkAsync(db, row, cancellationToken))
            throw new InvalidOperationException("A maintenance request linked to a posted ledger transaction cannot be changed.");

        var now = DateTime.UtcNow;
        var previousStatus = row.Status;
        row.Title = title.Trim();
        row.Description = description?.Trim();
        row.CreatedAtUtc = new DateTime(requestDate.Year, requestDate.Month, requestDate.Day, 12, 0, 0, DateTimeKind.Utc);
        row.UpdatedAtUtc = now;
        row.Status = newStatus;
        row.ClosedAtUtc = newStatus == Done || newStatus == Cancelled ? now : null;
        if (previousStatus != newStatus)
            row.StatusEvents.Add(new MaintenanceStatusEventEntity
            {
                StatusEventId = Guid.NewGuid(), RequestId = row.RequestId,
                FromStatus = previousStatus, ToStatus = newStatus,
                ActorReference = "admin", OccurredAtUtc = now
            });

        var currentVendor = row.Assignments.OrderByDescending(x => x.AssignedAtUtc)
            .Select(x => x.VendorName).FirstOrDefault() ?? "";
        var nextVendor = vendor?.Trim() ?? "";
        if (!string.Equals(currentVendor, nextVendor, StringComparison.Ordinal))
            row.Assignments.Add(new MaintenanceAssignmentEntity
            {
                AssignmentId = Guid.NewGuid(), RequestId = row.RequestId,
                AssigneeReference = "admin", VendorName = string.IsNullOrWhiteSpace(nextVendor) ? null : nextVendor,
                AssignedAtUtc = now
            });

        await db.SaveChangesAsync(cancellationToken);
        return await MapAsync(db, row, cancellationToken);
    }

    /// <summary>Deletes a request only when it has no posted financial link or stored attachments.</summary>
    public async Task<bool> DeleteAsync(
        string buildingId, string id, CancellationToken cancellationToken = default)
    {
        ValidateBuildingId(buildingId);
        var requestId = ParseId(id);
        if (requestId is null) return false;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var row = await db.MaintenanceRequests.Include(x => x.Assignments)
            .Include(x => x.Comments).Include(x => x.Attachments)
            .Include(x => x.StatusEvents).Include(x => x.LedgerLinks)
            .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.RequestId == requestId, cancellationToken);
        if (row is null) return false;
        if (await HasPostedLedgerLinkAsync(db, row, cancellationToken))
            throw new InvalidOperationException("A maintenance request linked to a posted ledger transaction cannot be deleted.");
        if (row.Attachments.Count > 0)
            throw new InvalidOperationException("A maintenance request with stored attachments cannot be deleted.");
        db.MaintenanceRequests.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Links a maintenance request to its posted expense transaction.</summary>
    public async Task<bool> MarkAddedToExpenseAsync(
        string buildingId, string id, CancellationToken cancellationToken = default)
    {
        ValidateBuildingId(buildingId);
        var requestId = ParseId(id);
        if (requestId is null) return false;
        var sourceKey = requestId.Value.ToString("D");
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var row = await db.MaintenanceRequests.Include(x => x.LedgerLinks)
            .SingleOrDefaultAsync(x => x.BuildingId == buildingId && x.RequestId == requestId, cancellationToken);
        if (row is null) return false;
        var transaction = await db.LedgerTransactions.AsNoTracking().SingleOrDefaultAsync(x =>
            x.BuildingId == buildingId && x.SourceType == "Expense" &&
            x.SourceKey == sourceKey && x.Kind == LedgerTransactionKind.Expense &&
            x.Status == LedgerTransactionStatus.Posted, cancellationToken);
        if (transaction is null)
            throw new InvalidOperationException("The posted expense transaction could not be found; no maintenance link was created.");
        if (row.LedgerLinks.Any(x => x.LedgerTransactionId == transaction.LedgerTransactionId)) return true;
        db.MaintenanceLedgerLinks.Add(new MaintenanceLedgerLinkEntity
        {
            LinkId = Guid.NewGuid(), RequestId = row.RequestId,
            LedgerTransactionId = transaction.LedgerTransactionId, LinkType = 1,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Gets the display name of a building.</summary>
    public async Task<string?> GetBuildingNameAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        ValidateBuildingId(buildingId);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Buildings.AsNoTracking().Where(x => x.Id == buildingId)
            .Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Gets active apartment identifiers for a building.</summary>
    public async Task<List<string>> GetActiveApartmentIdsAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        ValidateBuildingId(buildingId);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Apartments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && !x.IsClosed && !x.IsDisabled)
            .OrderBy(x => x.Number).Select(x => x.Id).ToListAsync(cancellationToken);
    }

    private static async Task<bool> HasPostedLedgerLinkAsync(
        AppDbContext db, MaintenanceRequestEntity row, CancellationToken cancellationToken)
    {
        var linkedIds = row.LedgerLinks.Select(x => x.LedgerTransactionId).ToArray();
        var sourceKey = row.RequestId.ToString("D");
        return await db.LedgerTransactions.AsNoTracking().AnyAsync(x =>
            x.BuildingId == row.BuildingId && x.Status == LedgerTransactionStatus.Posted &&
            (linkedIds.Contains(x.LedgerTransactionId) ||
             (x.Kind == LedgerTransactionKind.Expense && x.SourceType == "Expense" && x.SourceKey == sourceKey)),
            cancellationToken);
    }

    private static async Task<MaintenanceRecord> MapAsync(
        AppDbContext db, MaintenanceRequestEntity row, CancellationToken cancellationToken)
    {
        var links = row.LedgerLinks.Select(x => x.LedgerTransactionId).ToArray();
        var sourceKey = row.RequestId.ToString("D");
        var postedIds = await db.LedgerTransactions.AsNoTracking()
            .Where(x => x.BuildingId == row.BuildingId && x.Status == LedgerTransactionStatus.Posted &&
                (links.Contains(x.LedgerTransactionId) ||
                 (x.Kind == LedgerTransactionKind.Expense && x.SourceType == "Expense" && x.SourceKey == sourceKey)))
            .Select(x => x.LedgerTransactionId).Distinct().ToArrayAsync(cancellationToken);
        var cost = postedIds.Length == 0 ? 0m :
            await db.LedgerEntries.AsNoTracking()
                .Where(x => postedIds.Contains(x.LedgerTransactionId) && x.BuildingId == row.BuildingId)
                .SumAsync(x => x.SignedAmount < 0 ? -x.SignedAmount : x.SignedAmount, cancellationToken);
        var vendor = row.Assignments.OrderByDescending(x => x.AssignedAtUtc)
            .Select(x => x.VendorName).FirstOrDefault() ?? "";
        return new MaintenanceRecord
        {
            Id = "mnt-" + row.RequestId.ToString("N"),
            Title = row.Title,
            Description = row.Description ?? "",
            Date = row.CreatedAtUtc.ToString("yyyy-MM-dd"),
            Cost = cost,
            Vendor = vendor,
            Status = FormatStatus(row.Status),
            CreatedBy = row.RequestedByReference ?? "",
            CreatedAt = row.CreatedAtUtc.ToString("O"),
            AddedToExpense = postedIds.Length > 0,
            AddedToExpenseAt = postedIds.Length > 0
                ? row.LedgerLinks.Where(x => postedIds.Contains(x.LedgerTransactionId))
                    .Select(x => (DateTime?)x.CreatedAtUtc).Min()?.ToString("O")
                : null
        };
    }

    private static Guid? ParseId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var value = id.StartsWith("mnt-", StringComparison.OrdinalIgnoreCase) ? id[4..] : id;
        return Guid.TryParse(value, out var parsed) && parsed != Guid.Empty ? parsed : null;
    }

    private static byte ParseStatus(string status) => status switch
    {
        "open" => Open,
        "inprogress" => InProgress,
        "done" => Done,
        "cancelled" => Cancelled,
        _ => throw new ArgumentException("Maintenance status is invalid.", nameof(status))
    };

    private static string FormatStatus(byte status) => status switch
    {
        Open => "open", InProgress => "inprogress", Done => "done", Cancelled => "cancelled",
        _ => "open"
    };

    private static string NormalizeStatus(string? status, bool allowEmpty)
    {
        var value = string.IsNullOrWhiteSpace(status) && allowEmpty
            ? "open" : (status ?? "").Trim().ToLowerInvariant();
        _ = ParseStatus(value);
        return value;
    }

    private static void ValidateNoUnpostedCost(decimal cost)
    {
        if (cost != 0m)
            throw new ArgumentException("Maintenance cost is recorded only when the expense is posted to the ledger.", nameof(cost));
    }

    private static void ValidateBuildingId(string buildingId)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || buildingId.Length > 64)
            throw new ArgumentException("A valid building id is required.", nameof(buildingId));
    }

    private static void ValidateText(string? value, int maxLength, string parameterName, bool required = false)
    {
        if (required && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A value is required.", parameterName);
        if (value?.Length > maxLength)
            throw new ArgumentException($"The {parameterName} field exceeds {maxLength} characters.", parameterName);
    }
}
