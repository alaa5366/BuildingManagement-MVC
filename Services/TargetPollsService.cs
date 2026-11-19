using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Target-schema poll workflow. All poll content and votes are stored in Polls,
/// PollOptions and PollVotes; no Building.Polls aggregate is read or written.
/// </summary>
public sealed class TargetPollsService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetPollsService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<Poll>> GetPageAsync(
        string buildingId, int page, int pageSize, bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.Polls.AsNoTracking().Where(p => p.BuildingId == buildingId);
        if (activeOnly) query = query.Where(p => !p.IsClosed);

        var total = await query.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, pages);

        var entities = await query
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Include(p => p.Options)
                .ThenInclude(o => o.Votes)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResult<Poll>
        {
            Items = entities.Select(Map).ToList(),
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<Poll?> GetAsync(string buildingId, string pollId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Polls.AsNoTracking()
            .Where(p => p.BuildingId == buildingId && p.Id == pollId)
            .Include(p => p.Options).ThenInclude(o => o.Votes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<Poll> CreateAsync(
        string buildingId, string question, string? description,
        IReadOnlyCollection<string> optionTexts, string? deadline, string createdBy,
        CancellationToken cancellationToken = default)
    {
        var options = optionTexts.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim()).Distinct(StringComparer.Ordinal).ToList();
        if (string.IsNullOrWhiteSpace(question)) throw new ArgumentException("Poll question is required.");
        if (options.Count < 2) throw new ArgumentException("At least two distinct poll options are required.");
        if (!string.IsNullOrWhiteSpace(deadline) &&
            !DateTime.TryParse(deadline, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out _))
            throw new ArgumentException("Poll deadline is invalid.");

        var now = DateTime.UtcNow;
        var poll = new PollEntity
        {
            BuildingId = buildingId,
            Id = "poll-" + Guid.NewGuid().ToString("N"),
            Question = question.Trim(),
            Description = description?.Trim() ?? "",
            CreatedBy = createdBy,
            CreatedAt = now,
            Deadline = string.IsNullOrWhiteSpace(deadline) ? null :
                DateTime.Parse(deadline, System.Globalization.CultureInfo.InvariantCulture).Date,
            IsClosed = false,
            Options = options.Select((text, index) => new PollOptionEntity
            {
                BuildingId = buildingId,
                PollId = "pending",
                Id = "opt-" + Guid.NewGuid().ToString("N"),
                Text = text,
                SortOrder = index
            }).ToList()
        };
        foreach (var option in poll.Options) option.PollId = poll.Id;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        db.Polls.Add(poll);
        await db.SaveChangesAsync(cancellationToken);
        return Map(poll);
    }

    public async Task<bool> SetClosedAsync(
        string buildingId, string pollId, bool closed, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var poll = await db.Polls.FirstOrDefaultAsync(
            p => p.BuildingId == buildingId && p.Id == pollId, cancellationToken);
        if (poll is null) return false;
        poll.IsClosed = closed;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string buildingId, string pollId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var poll = await db.Polls.FirstOrDefaultAsync(
            p => p.BuildingId == buildingId && p.Id == pollId, cancellationToken);
        if (poll is null) return false;
        db.Polls.Remove(poll);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> VoteAsync(
        string buildingId, string pollId, string apartmentId, string optionId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var eligible = await db.Polls.AnyAsync(p =>
            p.BuildingId == buildingId && p.Id == pollId && !p.IsClosed &&
            (p.Deadline == null || p.Deadline >= now) &&
            p.Options.Any(o => o.Id == optionId), cancellationToken) &&
            await db.Apartments.AsNoTracking().AnyAsync(a =>
                a.BuildingId == buildingId && a.Id == apartmentId && !a.IsClosed && !a.IsDisabled,
                cancellationToken);
        if (!eligible) return false;

        db.PollVotes.Add(new PollVoteEntity
        {
            BuildingId = buildingId,
            PollId = pollId,
            ApartmentId = apartmentId,
            OptionId = optionId,
            VotedAt = now
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The composite key makes concurrent duplicate submissions safe.
            var alreadyVoted = await db.PollVotes.AsNoTracking().AnyAsync(v =>
                v.BuildingId == buildingId && v.PollId == pollId && v.ApartmentId == apartmentId,
                cancellationToken);
            if (alreadyVoted) return false;
            throw;
        }
    }

    public async Task<(string Name, int ActiveApartmentCount)?> GetBuildingSummaryAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var name = await db.Buildings.AsNoTracking().Where(x => x.Id == buildingId)
            .Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
        if (name is null) return null;
        var activeApartments = await db.Apartments.AsNoTracking()
            .CountAsync(x => x.BuildingId == buildingId && !x.IsClosed, cancellationToken);
        return (name, activeApartments);
    }

    public async Task<bool> IsActiveApartmentAsync(
        string buildingId, string apartmentId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Apartments.AsNoTracking().AnyAsync(
            x => x.BuildingId == buildingId && x.Id == apartmentId && !x.IsClosed && !x.IsDisabled,
            cancellationToken);
    }

    private static Poll Map(PollEntity entity) => new()
    {
        Id = entity.Id,
        Question = entity.Question,
        Description = entity.Description,
        CreatedBy = entity.CreatedBy,
        CreatedAt = entity.CreatedAt.ToString("O"),
        Deadline = entity.Deadline?.ToString("yyyy-MM-dd"),
        Closed = entity.IsClosed,
        Options = entity.Options.OrderBy(o => o.SortOrder).ThenBy(o => o.Id)
            .Select(o => new PollOption { Id = o.Id, Text = o.Text }).ToList(),
        Votes = entity.Options.SelectMany(o => o.Votes)
            .GroupBy(v => v.ApartmentId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().OptionId, StringComparer.Ordinal)
    };
}
