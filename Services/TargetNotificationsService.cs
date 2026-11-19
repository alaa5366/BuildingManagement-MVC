using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Reads and updates notifications in the canonical SQL notification store.</summary>
public sealed class TargetNotificationsService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetNotificationsService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<List<Notification>> GetForAdminAsync(
        string buildingId, int max = 500, CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, null);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var items = await db.Notifications.AsNoTracking()
            .Where(item => item.BuildingId == buildingId && item.RecipientType == "admin")
            .OrderByDescending(item => item.Ts)
            .Take(Math.Clamp(max, 1, 500))
            .ToListAsync(cancellationToken);
        return items.Select(ToModel).ToList();
    }

    public async Task<List<Notification>> GetForResidentAsync(
        string buildingId, string apartmentId, int max = 500,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, apartmentId);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var items = await db.Notifications.AsNoTracking()
            .Where(item => item.BuildingId == buildingId &&
                item.RecipientType == "resident" && item.RecipientApartmentId == apartmentId)
            .OrderByDescending(item => item.Ts)
            .Take(Math.Clamp(max, 1, 500))
            .ToListAsync(cancellationToken);
        return items.Select(ToModel).ToList();
    }


    public async Task<PagedResult<Notification>> GetPageForAdminAsync(
        string buildingId, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await GetPageAsync(buildingId, "admin", null, page, pageSize, cancellationToken);

    public async Task<PagedResult<Notification>> GetPageForResidentAsync(
        string buildingId, string apartmentId, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        await GetPageAsync(buildingId, "resident", apartmentId, page, pageSize, cancellationToken);

    private async Task<PagedResult<Notification>> GetPageAsync(
        string buildingId, string recipientType, string? apartmentId, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        ValidateScope(buildingId, recipientType == "resident" ? apartmentId : null);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.Notifications.AsNoTracking().Where(item =>
            item.BuildingId == buildingId && item.RecipientType == recipientType &&
            (recipientType == "admin"
                ? item.RecipientApartmentId == null
                : item.RecipientApartmentId == apartmentId));

        var total = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);
        var items = await query.OrderByDescending(item => item.Ts).ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<Notification>
        {
            Items = items.Select(ToModel).ToList(),
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task CreateResidentDepositNoticeAsync(
        string buildingId,
        string apartmentId,
        string title,
        string body,
        CancellationToken cancellationToken = default,
        string type = "deposit",
        string icon = "💳")
    {
        ValidateScope(buildingId, apartmentId);
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
            throw new ArgumentException("A notification title of 1 to 200 characters is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(body) || body.Length > 1000)
            throw new ArgumentException("A notification body of 1 to 1000 characters is required.", nameof(body));
        if (string.IsNullOrWhiteSpace(type) || type.Length > 40)
            throw new ArgumentException("A notification type of 1 to 40 characters is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(icon) || icon.Length > 16)
            throw new ArgumentException("A notification icon of 1 to 16 characters is required.", nameof(icon));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var apartmentExists = await db.Apartments.AsNoTracking().AnyAsync(
            apartment => apartment.BuildingId == buildingId && apartment.Id == apartmentId,
            cancellationToken);
        if (!apartmentExists)
            throw new InvalidOperationException("The notification recipient apartment is outside the building scope.");

        db.Notifications.Add(new NotificationEntity
        {
            BuildingId = buildingId,
            Id = Guid.NewGuid().ToString("N"),
            Ts = DateTime.UtcNow,
            RecipientType = "resident",
            RecipientApartmentId = apartmentId,
            Type = type.Trim(),
            Icon = icon,
            Title = title.Trim(),
            Body = body.Trim(),
            IsRead = false
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> MarkReadAsync(
        string buildingId, string id, string recipientType, string? apartmentId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, recipientType == "resident" ? apartmentId : null);
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64) return false;
        if (recipientType is not ("admin" or "resident")) return false;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var item = await db.Notifications.SingleOrDefaultAsync(notification =>
            notification.BuildingId == buildingId && notification.Id == id &&
            notification.RecipientType == recipientType &&
            (recipientType == "admin"
                ? notification.RecipientApartmentId == null
                : notification.RecipientApartmentId == apartmentId), cancellationToken);
        if (item is null) return false;
        if (item.IsRead) return true;
        item.IsRead = true;
        item.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkAllReadAsync(
        string buildingId, string recipientType, string? apartmentId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, recipientType == "resident" ? apartmentId : null);
        if (recipientType is not ("admin" or "resident"))
            throw new ArgumentException("Unsupported notification recipient type.", nameof(recipientType));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.Notifications.Where(item => item.BuildingId == buildingId &&
            item.RecipientType == recipientType && !item.IsRead);
        if (recipientType == "admin")
            query = query.Where(item => item.RecipientApartmentId == null);
        else
            query = query.Where(item => item.RecipientApartmentId == apartmentId);

        var now = DateTime.UtcNow;
        return await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(item => item.IsRead, true)
                .SetProperty(item => item.ReadAt, now),
            cancellationToken);
    }

    private static Notification ToModel(NotificationEntity item) => new()
    {
        Id = item.Id,
        Ts = item.Ts.ToString("O"),
        RecipientType = item.RecipientType,
        RecipientId = item.RecipientApartmentId,
        Type = item.Type,
        Icon = item.Icon,
        Title = item.Title,
        Body = item.Body,
        Read = item.IsRead,
        ReadAt = item.ReadAt?.ToString("O")
    };

    private static void ValidateScope(string buildingId, string? apartmentId)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || buildingId.Length > 64)
            throw new ArgumentException("A valid building scope is required.", nameof(buildingId));
        if (apartmentId is { Length: > 64 } || apartmentId is not null && string.IsNullOrWhiteSpace(apartmentId))
            throw new ArgumentException("A valid apartment scope is required.", nameof(apartmentId));
    }
}
