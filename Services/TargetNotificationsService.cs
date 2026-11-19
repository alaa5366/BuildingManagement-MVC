using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Uses the normalized NotificationMessages/Recipients/ReadStates store.</summary>
public sealed class TargetNotificationsService
{
    private const byte AdminRecipient = 0;
    private const byte ResidentRecipient = 1;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetNotificationsService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public Task<List<Notification>> GetForAdminAsync(
        string buildingId, int max = 500, CancellationToken cancellationToken = default) =>
        GetListAsync(buildingId, AdminRecipient, null, max, cancellationToken);

    public Task<List<Notification>> GetForResidentAsync(
        string buildingId, string apartmentId, int max = 500,
        CancellationToken cancellationToken = default) =>
        GetListAsync(buildingId, ResidentRecipient, apartmentId, max, cancellationToken);

    public Task<PagedResult<Notification>> GetPageForAdminAsync(
        string buildingId, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(buildingId, AdminRecipient, null, page, pageSize, cancellationToken);

    public Task<PagedResult<Notification>> GetPageForResidentAsync(
        string buildingId, string apartmentId, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(buildingId, ResidentRecipient, apartmentId, page, pageSize, cancellationToken);

    private async Task<List<Notification>> GetListAsync(
        string buildingId, byte recipientType, string? apartmentId, int max,
        CancellationToken cancellationToken)
    {
        ValidateScope(buildingId, recipientType == ResidentRecipient ? apartmentId : null);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var rows = await RecipientQuery(db, buildingId, recipientType, apartmentId)
            .OrderByDescending(x => x.Message!.CreatedAtUtc)
            .ThenByDescending(x => x.MessageId)
            .Take(Math.Clamp(max, 1, 500))
            .Include(x => x.Message)
            .Include(x => x.ReadState)
            .ToListAsync(cancellationToken);
        return rows.Select(ToModel).ToList();
    }

    private async Task<PagedResult<Notification>> GetPageAsync(
        string buildingId, byte recipientType, string? apartmentId, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        ValidateScope(buildingId, recipientType == ResidentRecipient ? apartmentId : null);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = RecipientQuery(db, buildingId, recipientType, apartmentId);
        var total = await query.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, pages);
        var rows = await query.OrderByDescending(x => x.Message!.CreatedAtUtc)
            .ThenByDescending(x => x.MessageId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(x => x.Message).Include(x => x.ReadState)
            .ToListAsync(cancellationToken);
        return new PagedResult<Notification>
        {
            Items = rows.Select(ToModel).ToList(),
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    private static IQueryable<NotificationRecipientEntity> RecipientQuery(
        AppDbContext db, string buildingId, byte recipientType, string? apartmentId)
    {
        var query = db.NotificationRecipients.Where(x =>
            x.RecipientType == recipientType && x.Message!.BuildingId == buildingId);
        if (recipientType == ResidentRecipient)
            query = query.Where(x => x.ApartmentId == apartmentId);
        return query;
    }

    public Task CreateResidentDepositNoticeAsync(
        string buildingId, string apartmentId, string title, string body,
        CancellationToken cancellationToken = default, string type = "deposit", string icon = "💳") =>
        CreateResidentNotificationAsync(buildingId, apartmentId, title, body, type, icon, cancellationToken);

    public async Task CreateResidentNotificationAsync(
        string buildingId, string apartmentId, string title, string body,
        string type = "info", string icon = "🔔",
        CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, apartmentId);
        if (string.IsNullOrWhiteSpace(title) || title.Length > 250)
            throw new ArgumentException("A notification title of 1 to 250 characters is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(body) || body.Length > 4000)
            throw new ArgumentException("A notification body of 1 to 4000 characters is required.", nameof(body));
        if (string.IsNullOrWhiteSpace(type) || type.Length > 80)
            throw new ArgumentException("A notification type of 1 to 80 characters is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(icon) || icon.Length > 16)
            throw new ArgumentException("A notification icon of 1 to 16 characters is required.", nameof(icon));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var apartmentExists = await db.Apartments.AsNoTracking().AnyAsync(
            x => x.BuildingId == buildingId && x.Id == apartmentId, cancellationToken);
        if (!apartmentExists)
            throw new InvalidOperationException("The notification recipient apartment is outside the building scope.");

        var now = DateTime.UtcNow;
        db.NotificationMessages.Add(new NotificationMessageEntity
        {
            MessageId = Guid.NewGuid(),
            BuildingId = buildingId,
            Title = title.Trim(),
            Body = body.Trim(),
            Channel = 0,
            Origin = type.Trim(),
            CreatedAtUtc = now,
            Recipients =
            {
                new NotificationRecipientEntity
                {
                    RecipientId = Guid.NewGuid(),
                    RecipientType = ResidentRecipient,
                    RecipientReference = apartmentId,
                    ApartmentId = apartmentId,
                    CreatedAtUtc = now
                }
            }
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> MarkReadAsync(
        string buildingId, string id, string recipientType, string? apartmentId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, recipientType == "resident" ? apartmentId : null);
        if (!Guid.TryParseExact(id, "N", out var messageId) &&
            !Guid.TryParse(id, out messageId)) return false;
        var kind = recipientType switch
        {
            "admin" => AdminRecipient,
            "resident" => ResidentRecipient,
            _ => (byte)255
        };
        if (kind == 255) return false;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var recipient = await RecipientQuery(db, buildingId, kind, apartmentId)
            .Where(x => x.MessageId == messageId)
            .Include(x => x.ReadState)
            .SingleOrDefaultAsync(cancellationToken);
        if (recipient is null) return false;
        if (recipient.ReadState?.ReadAtUtc is not null) return true;
        var now = DateTime.UtcNow;
        if (recipient.ReadState is null)
            recipient.ReadState = new NotificationReadStateEntity
            {
                ReadStateId = Guid.NewGuid(),
                RecipientId = recipient.RecipientId,
                ReadAtUtc = now
            };
        else
            recipient.ReadState.ReadAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<int> GetUnreadCountForAdminAsync(
        string buildingId, CancellationToken cancellationToken = default) =>
        GetUnreadCountAsync(buildingId, AdminRecipient, null, cancellationToken);

    public Task<int> GetUnreadCountForResidentAsync(
        string buildingId, string apartmentId, CancellationToken cancellationToken = default) =>
        GetUnreadCountAsync(buildingId, ResidentRecipient, apartmentId, cancellationToken);

    private async Task<int> GetUnreadCountAsync(
        string buildingId, byte recipientType, string? apartmentId, CancellationToken cancellationToken)
    {
        ValidateScope(buildingId, recipientType == ResidentRecipient ? apartmentId : null);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await RecipientQuery(db, buildingId, recipientType, apartmentId)
            .CountAsync(x => x.ReadState == null || x.ReadState.ReadAtUtc == null, cancellationToken);
    }

    public async Task<int> MarkAllReadAsync(
        string buildingId, string recipientType, string? apartmentId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, recipientType == "resident" ? apartmentId : null);
        var kind = recipientType switch
        {
            "admin" => AdminRecipient,
            "resident" => ResidentRecipient,
            _ => (byte)255
        };
        if (kind == 255) throw new ArgumentException("Unsupported notification recipient type.", nameof(recipientType));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var rows = await RecipientQuery(db, buildingId, kind, apartmentId)
            .Include(x => x.ReadState)
            .Where(x => x.ReadState == null || x.ReadState.ReadAtUtc == null)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0) return 0;
        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            if (row.ReadState is null)
                row.ReadState = new NotificationReadStateEntity
                {
                    ReadStateId = Guid.NewGuid(),
                    RecipientId = row.RecipientId,
                    ReadAtUtc = now
                };
            else
                row.ReadState.ReadAtUtc = now;
        }
        await db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private static Notification ToModel(NotificationRecipientEntity row) => new()
    {
        Id = row.MessageId.ToString("N"),
        Ts = row.Message!.CreatedAtUtc.ToString("O"),
        RecipientType = row.RecipientType == AdminRecipient ? "admin" : "resident",
        RecipientId = row.ApartmentId,
        Type = row.Message.Origin,
        Icon = "🔔",
        Title = row.Message.Title ?? "",
        Body = row.Message.Body,
        Read = row.ReadState?.ReadAtUtc is not null,
        ReadAt = row.ReadState?.ReadAtUtc?.ToString("O")
    };

    private static void ValidateScope(string buildingId, string? apartmentId)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || buildingId.Length > 64)
            throw new ArgumentException("A valid building scope is required.", nameof(buildingId));
        if (apartmentId is { Length: > 64 } ||
            apartmentId is not null && string.IsNullOrWhiteSpace(apartmentId))
            throw new ArgumentException("A valid apartment scope is required.", nameof(apartmentId));
    }
}
