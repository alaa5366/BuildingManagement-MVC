using System.Security.Claims;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Resolves the authenticated legacy web principal to one canonical target account.
/// Resolution fails closed unless the email maps to exactly one active target account.
/// </summary>
public sealed class TargetAccountIdentityResolver
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetAccountIdentityResolver(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<Guid?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email)) return null;

        var normalizedEmail = email.Trim().ToUpperInvariant();
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
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
