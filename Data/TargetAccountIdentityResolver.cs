using System.Security.Claims;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Resolves a target account from its immutable subject claim, with a unique-email fallback for legacy cookies.
/// Resolution fails closed for inactive, archived, missing, or ambiguous identities.
/// </summary>
public sealed class TargetAccountIdentityResolver
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetAccountIdentityResolver(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>Resolves the target-system account corresponding to the current principal.</summary>
    public async Task<Guid?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        // Target-authenticated accounts use their immutable AccountId as the subject.
        // Resolve it first so phone/PIN accounts do not depend on having an email.
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (Guid.TryParse(subject, out var accountId))
        {
            var activeSubject = await (
                from account in db.UserAccounts.AsNoTracking()
                join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
                where account.AccountId == accountId &&
                      account.AccountStatus == 1 && !person.IsArchived
                select account.AccountId)
                .SingleOrDefaultAsync(cancellationToken);
            return activeSubject == Guid.Empty ? null : activeSubject;
        }

        // Legacy cookie identities may still carry only an email claim.
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return null;

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var matches = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.NormalizedEmail == normalizedEmail &&
                  account.AccountStatus == 1 && !person.IsArchived
            select account.AccountId)
            .Take(2)
            .ToListAsync(cancellationToken);

        return matches.Count == 1 ? matches[0] : null;
    }
}
