using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Authenticates resident accounts against the target identity tables.</summary>
public sealed class IdentityAccountAuthenticationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPasswordHasher _hasher;

    public IdentityAccountAuthenticationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IPasswordHasher hasher)
    {
        _dbFactory = dbFactory;
        _hasher = hasher;
    }

    /// <summary>Returns null only when the identifier is absent from the target identity tables.</summary>
    public async Task<AuthResult?> AuthenticateResidentAsync(string identifier, string credential)
    {
        var email = identifier.Trim().ToUpperInvariant();
        var phone = AuthHelpers.NormalizePhone(identifier);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var matches = await (
            from account in db.UserAccounts
            join person in db.Persons on account.PersonId equals person.PersonId
            where account.NormalizedEmail == email || person.PrimaryPhone == phone
            select new { Account = account, Person = person })
            .Take(2)
            .ToListAsync();

        if (matches.Count == 0) return null;
        if (matches.Count != 1) return AuthResult.Fail("wrong-credentials");

        var match = matches[0];
        if (match.Account.AccountStatus != 1 || match.Person.IsArchived)
            return AuthResult.Fail("wrong-credentials");

        var now = DateTime.UtcNow;
        var roleExists = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
            where assignment.AccountId == match.Account.AccountId
                  && assignment.RoleKey == "resident"
                  && role.IsActive
                  && assignment.ValidFromUtc <= now
                  && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                  && assignment.RevokedAtUtc == null
            select assignment.RoleAssignmentId).AnyAsync();
        if (!roleExists) return AuthResult.Fail("wrong-credentials");

        var storedCredential = await db.UserCredentials.AsNoTracking()
            .Where(item => item.AccountId == match.Account.AccountId
                && item.RevokedAtUtc == null
                && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync();
        if (storedCredential == null ||
            (storedCredential.HashAlgorithm != "BCrypt.Net-Next" && storedCredential.HashAlgorithm != "bcrypt-v1") ||
            !_hasher.Verify(credential, storedCredential.CredentialHash))
            return AuthResult.Fail("wrong-credentials");

        var occupancy = await db.ApartmentOccupancies.AsNoTracking()
            .Where(item => item.PersonId == match.Person.PersonId
                && item.ValidFromUtc <= now
                && (item.ValidToUtc == null || item.ValidToUtc > now))
            .OrderByDescending(item => item.IsPrimary)
            .ThenBy(item => item.ValidFromUtc)
            .Select(item => new { item.BuildingId, item.ApartmentId })
            .FirstOrDefaultAsync();

        match.Account.LastLoginAtUtc = now;
        await db.SaveChangesAsync();

        var result = AuthResult.Ok(
            uid: match.Account.AccountId.ToString("D"),
            role: "resident",
            email: match.Account.NormalizedEmail ?? "",
            name: match.Person.DisplayName,
            buildingIds: occupancy == null ? new List<string>() : new List<string> { occupancy.BuildingId },
            aptId: occupancy?.ApartmentId);
        result.UsesTargetIdentity = true;
        result.SecurityStamp = match.Account.SecurityStamp;
        return result;
    }
}
