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

        var unread = await query.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var item in unread)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }
        await db.SaveChangesAsync(cancellationToken);
        return unread.Count;
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
