using System.Data;
using System.Security.Cryptography;
using System.Text;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Persists poll definitions and anonymous ballots using normalized operational voting tables.</summary>
public sealed class TargetPollsService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IDataProtector _choiceProtector;

    /// <summary>Initializes the service with its database factory and ballot-payload protector.</summary>
    public TargetPollsService(IDbContextFactory<AppDbContext> factory, IDataProtectionProvider protectionProvider)
    {
        _factory = factory;
        _choiceProtector = protectionProvider.CreateProtector("BuildingManagement.Polls.ChoicePayload.v1");
    }

    /// <summary>Gets a paged list of polls and aggregate results for the requested building.</summary>
    public async Task<PagedResult<Poll>> GetPageAsync(string buildingId, int page, int pageSize, bool activeOnly,
        CancellationToken cancellationToken = default, string? subjectReference = null)
    {
        ValidateScope(buildingId, nameof(buildingId));
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var query = db.Polls.AsNoTracking().Where(p => p.BuildingId == buildingId);
        if (activeOnly)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(p => !p.IsClosed && (p.Deadline == null || p.Deadline.Value.Date >= today));
        }
        var total = await query.CountAsync(cancellationToken);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, pages);
        var entities = await query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Include(p => p.Options).AsSplitQuery()
            .ToListAsync(cancellationToken);
        var ids = entities.Select(x => x.Id).ToArray();
        var ballots = ids.Length == 0 ? new List<PollBallotEntity>() :
            await db.PollBallots.AsNoTracking().Where(x => x.BuildingId == buildingId && ids.Contains(x.PollId))
                .ToListAsync(cancellationToken);
        var entitlements = string.IsNullOrWhiteSpace(subjectReference) || ids.Length == 0
            ? new List<PollEligibilityEntity>()
            : await db.PollEligibility.AsNoTracking().Where(x => x.BuildingId == buildingId &&
                x.SubjectReference == subjectReference && ids.Contains(x.PollId)).ToListAsync(cancellationToken);
        return new PagedResult<Poll>
        {
            Items = entities.Select(x => Map(x, ballots.Where(b => b.PollId == x.Id),
                entitlements.FirstOrDefault(e => e.PollId == x.Id))).ToList(),
            CurrentPage = page, PageSize = pageSize, TotalItems = total
        };
    }

    /// <summary>Gets one poll and its aggregate results within the requested building.</summary>
    public async Task<Poll?> GetAsync(string buildingId, string pollId, CancellationToken cancellationToken = default,
        string? subjectReference = null)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Polls.AsNoTracking().Where(p => p.BuildingId == buildingId && p.Id == pollId)
            .Include(p => p.Options).FirstOrDefaultAsync(cancellationToken);
        if (entity is null) return null;
        var ballots = await db.PollBallots.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.PollId == pollId).ToListAsync(cancellationToken);
        var entitlement = string.IsNullOrWhiteSpace(subjectReference) ? null :
            await db.PollEligibility.AsNoTracking().FirstOrDefaultAsync(x => x.BuildingId == buildingId &&
                x.PollId == pollId && x.SubjectReference == subjectReference, cancellationToken);
        return Map(entity, ballots, entitlement);
    }

    /// <summary>Creates a poll, its versioned policy, and one eligibility record per active apartment.</summary>
    public async Task<Poll> CreateAsync(string buildingId, string question, string? description,
        IReadOnlyCollection<string> optionTexts, string? deadline, string createdBy,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, nameof(buildingId));
        ValidateText(question, 300, nameof(question), true);
        ValidateText(description, 2000, nameof(description));
        ValidateText(createdBy, 160, nameof(createdBy), true);
        var options = optionTexts.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
            .Distinct(StringComparer.Ordinal).ToList();
        if (options.Count < 2) throw new ArgumentException("At least two distinct poll options are required.");
        if (options.Count > 20) throw new ArgumentException("A poll cannot contain more than 20 options.");
        if (options.Any(x => x.Length > 200)) throw new ArgumentException("Poll options cannot exceed 200 characters.");
        DateTime? due = null;
        if (!string.IsNullOrWhiteSpace(deadline))
        {
            if (!DateTime.TryParseExact(deadline, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
                throw new ArgumentException("Poll deadline must use yyyy-MM-dd.");
            due = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        }

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        if (!await db.Buildings.AsNoTracking().AnyAsync(x => x.Id == buildingId, cancellationToken))
            throw new ArgumentException("The requested building does not exist.", nameof(buildingId));
        var now = DateTime.UtcNow;
        var id = "poll-" + Guid.NewGuid().ToString("N");
        var poll = new PollEntity
        {
            BuildingId = buildingId, Id = id, Question = question.Trim(), Description = description?.Trim() ?? "",
            CreatedBy = createdBy.Trim(), CreatedAt = now, Deadline = due, IsClosed = false,
            Options = options.Select((text, i) => new PollOptionEntity
            {
                BuildingId = buildingId, PollId = id, Id = "opt-" + Guid.NewGuid().ToString("N"),
                Text = text, SortOrder = i
            }).ToList()
        };
        var apartments = await db.Apartments.AsNoTracking().Where(x => x.BuildingId == buildingId &&
            !x.IsClosed && !x.IsDisabled).Select(x => x.Id).ToListAsync(cancellationToken);
        db.Polls.Add(poll);
        db.PollVotingPolicies.Add(new PollVotingPolicyEntity
        {
            PolicyId = Guid.NewGuid(), BuildingId = buildingId, PollId = id, PolicyVersion = 1,
            VotingMode = 0, EligibilityMode = 0, IsActive = true, CreatedAtUtc = now
        });
        db.PollEligibility.AddRange(apartments.Select(apartmentId => new PollEligibilityEntity
        {
            EligibilityId = Guid.NewGuid(), BuildingId = buildingId, PollId = id, SubjectReference = apartmentId,
            IssuedAtUtc = now, ExpiresAtUtc = due?.AddDays(1) ?? now.AddYears(10)
        }));
        db.PollLifecycleEvents.Add(CreateEvent(buildingId, id, 1, createdBy, "Poll created", now));
        await db.SaveChangesAsync(cancellationToken);
        return Map(poll, Array.Empty<PollBallotEntity>(), null);
    }

    /// <summary>Changes whether the specified poll is closed and appends a lifecycle event.</summary>
    public async Task<bool> SetClosedAsync(string buildingId, string pollId, bool closed,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var poll = await db.Polls.FirstOrDefaultAsync(p => p.BuildingId == buildingId && p.Id == pollId, cancellationToken);
        if (poll is null) return false;
        if (poll.IsClosed == closed) return true;
        poll.IsClosed = closed;
        db.PollLifecycleEvents.Add(CreateEvent(buildingId, pollId, closed ? (byte)2 : (byte)3, null,
            closed ? "Poll closed" : "Poll reopened", DateTime.UtcNow));
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Deletes a poll only before its first ballot and retains lifecycle evidence.</summary>
    public async Task<bool> DeleteAsync(string buildingId, string pollId, CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, nameof(buildingId));
        ValidateScope(pollId, nameof(pollId));
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var poll = await db.Polls.FirstOrDefaultAsync(p => p.BuildingId == buildingId && p.Id == pollId, cancellationToken);
        if (poll is null) return false;
        if (await db.PollBallots.AsNoTracking().AnyAsync(x => x.BuildingId == buildingId && x.PollId == pollId, cancellationToken))
            return false;
        // A close/reopen or imported legacy-evidence event means the definition is retained history.
        if (await db.PollLifecycleEvents.AsNoTracking().AnyAsync(x =>
            x.BuildingId == buildingId && x.PollId == pollId &&
            (x.EventType != 1 || x.Reason != "Poll created"), cancellationToken))
            return false;
        var entitlements = await db.PollEligibility.Where(x => x.BuildingId == buildingId && x.PollId == pollId)
            .ToListAsync(cancellationToken);
        var policies = await db.PollVotingPolicies.Where(x => x.BuildingId == buildingId && x.PollId == pollId)
            .ToListAsync(cancellationToken);
        db.PollEligibility.RemoveRange(entitlements);
        db.PollVotingPolicies.RemoveRange(policies);
        db.PollLifecycleEvents.Add(CreateEvent(buildingId, pollId, 4, null, "Poll deleted before voting", DateTime.UtcNow));
        db.Polls.Remove(poll);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Consumes a resident entitlement and stores an anonymous protected ballot.</summary>
    public async Task<bool> VoteAsync(string buildingId, string pollId, string apartmentId, string optionId,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(buildingId, nameof(buildingId));
        ValidateScope(pollId, nameof(pollId));
        ValidateScope(apartmentId, nameof(apartmentId));
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var valid = await db.Polls.AsNoTracking().AnyAsync(p => p.BuildingId == buildingId && p.Id == pollId &&
            !p.IsClosed && (p.Deadline == null || p.Deadline.Value.Date >= now.Date) &&
            p.Options.Any(o => o.Id == optionId), cancellationToken);
        if (!valid ||
            !await db.Apartments.AsNoTracking().AnyAsync(a => a.BuildingId == buildingId && a.Id == apartmentId &&
                !a.IsClosed && !a.IsDisabled, cancellationToken) ||
            !await db.PollVotingPolicies.AsNoTracking().AnyAsync(p => p.BuildingId == buildingId &&
                p.PollId == pollId && p.IsActive, cancellationToken))
            return false;

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;

        var claimed = false;
        if (db.Database.IsRelational())
        {
            var affected = await db.PollEligibility
                .Where(x => x.BuildingId == buildingId && x.PollId == pollId &&
                    x.SubjectReference == apartmentId && x.ConsumedAtUtc == null &&
                    x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ConsumedAtUtc, (DateTime?)now),
                    cancellationToken);
            claimed = affected == 1;
        }
        else
        {
            var entitlement = await db.PollEligibility.FirstOrDefaultAsync(x =>
                x.BuildingId == buildingId && x.PollId == pollId && x.SubjectReference == apartmentId,
                cancellationToken);
            if (entitlement is not null && entitlement.ConsumedAtUtc is null &&
                entitlement.RevokedAtUtc is null && entitlement.ExpiresAtUtc > now)
            {
                entitlement.ConsumedAtUtc = now;
                claimed = true;
            }
        }

        if (!claimed)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var token = RandomNumberGenerator.GetBytes(32);
        var tokenHash = SHA256.HashData(token);
        CryptographicOperations.ZeroMemory(token);
        // Coarsen the ballot timestamp so it cannot be directly joined to the precise
        // eligibility-consumption timestamp. No apartment or actor reference is stored with the ballot.
        var ballotTimeBucket = new DateTime(
            now.Ticks - now.Ticks % TimeSpan.FromMinutes(15).Ticks, DateTimeKind.Utc);
        db.PollBallots.Add(new PollBallotEntity
        {
            BallotId = Guid.NewGuid(), BuildingId = buildingId, PollId = pollId,
            BallotTokenHash = tokenHash, ChoicePayload = Encoding.UTF8.GetBytes(_choiceProtector.Protect(optionId)),
            CastAtUtc = ballotTimeBucket, BallotVersion = 1
        });
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Gets the building name and count of active apartments.</summary>
    public async Task<(string Name, int ActiveApartmentCount)?> GetBuildingSummaryAsync(
        string buildingId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var name = await db.Buildings.AsNoTracking().Where(x => x.Id == buildingId).Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (name is null) return null;
        var count = await db.Apartments.AsNoTracking().CountAsync(x => x.BuildingId == buildingId &&
            !x.IsClosed && !x.IsDisabled, cancellationToken);
        return (name, count);
    }

    /// <summary>Determines whether an apartment is currently active.</summary>
    public async Task<bool> IsActiveApartmentAsync(string buildingId, string apartmentId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        return await db.Apartments.AsNoTracking().AnyAsync(x => x.BuildingId == buildingId &&
            x.Id == apartmentId && !x.IsClosed && !x.IsDisabled, cancellationToken);
    }

    private Poll Map(PollEntity entity, IEnumerable<PollBallotEntity> ballots, PollEligibilityEntity? entitlement)
    {
        var counts = entity.Options.ToDictionary(x => x.Id, _ => 0, StringComparer.Ordinal);
        var total = 0;
        foreach (var ballot in ballots)
        {
            try
            {
                var protectedChoice = Encoding.UTF8.GetString(ballot.ChoicePayload);
                var optionId = _choiceProtector.Unprotect(protectedChoice);
                if (counts.ContainsKey(optionId)) { counts[optionId]++; total++; }
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException(
                    $"A ballot for poll '{entity.Id}' cannot be decrypted. Verify the configured Data Protection key ring.", ex);
            }
        }
        return new Poll
        {
            Id = entity.Id, Question = entity.Question, Description = entity.Description, CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt.ToString("O"), Deadline = entity.Deadline?.ToString("yyyy-MM-dd"),
            Closed = entity.IsClosed, TotalVotes = total, HasVoted = entitlement?.ConsumedAtUtc is not null,
            Options = entity.Options.OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
                .Select(x => new PollOption { Id = x.Id, Text = x.Text, VoteCount = counts[x.Id] }).ToList()
        };
    }

    private static PollLifecycleEventEntity CreateEvent(string buildingId, string pollId, byte eventType,
        string? actor, string reason, DateTime occurredAt) => new()
    {
        EventId = Guid.NewGuid(), BuildingId = buildingId, PollId = pollId,
        EventType = eventType, OccurredAtUtc = occurredAt, ActorReference = actor, Reason = reason
    };

    private static void ValidateScope(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
            throw new ArgumentException("A valid poll scope is required.", parameterName);
    }

    private static void ValidateText(string? value, int maxLength, string parameterName, bool required = false)
    {
        if (required && string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A required poll field is missing.", parameterName);
        if (value?.Length > maxLength) throw new ArgumentException($"The {parameterName} field exceeds {maxLength} characters.", parameterName);
    }
}
