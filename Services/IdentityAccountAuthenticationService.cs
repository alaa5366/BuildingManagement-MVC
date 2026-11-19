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

    /// <summary>
    /// Returns null only when no target account matches the identifier. A matching but
    /// inactive, unauthorized, or incorrectly credentialed account fails closed.
    /// </summary>
    public async Task<AuthResult?> AuthenticateResidentAsync(
        string identifier,
        string credential,
        string? buildingId = null,
        string? apartmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier) || credential is null)
            return AuthResult.Fail("wrong-credentials");

        var email = identifier.Trim().ToUpperInvariant();
        var phone = AuthHelpers.NormalizePhone(identifier);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var matches = await (
            from account in db.UserAccounts
            join person in db.Persons on account.PersonId equals person.PersonId
            where account.NormalizedEmail == email ||
                  (!string.IsNullOrEmpty(phone) && person.PrimaryPhone == phone)
            select new { Account = account, Person = person })
            .Take(2)
            .ToListAsync(cancellationToken);

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
            select assignment.RoleAssignmentId)
            .AnyAsync(cancellationToken);
        // This service owns only the resident sign-in path for now. Let the caller
        // continue its explicitly retained legacy admin/super-admin path when the
        // matched target account has no active resident assignment.
        if (!roleExists) return null;

        // Do not let a newer expired/revoked credential shadow an older still-valid
        // credential. Credential material is opaque: the submitted value is never trimmed.
        var credentials = await db.UserCredentials.AsNoTracking()
            .Where(item => item.AccountId == match.Account.AccountId
                && item.RevokedAtUtc == null
                && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new { item.CredentialHash, item.HashAlgorithm })
            .Take(8)
            .ToListAsync(cancellationToken);

        var credentialValid = credentials.Any(item =>
            (item.HashAlgorithm == "BCrypt.Net-Next" || item.HashAlgorithm == "bcrypt-v1") &&
            _hasher.Verify(credential, item.CredentialHash));
        if (!credentialValid)
            return AuthResult.Fail("wrong-credentials");

        var occupancy = await db.ApartmentOccupancies.AsNoTracking()
            .Where(item => item.PersonId == match.Person.PersonId
                && item.ValidFromUtc <= now
                && (item.ValidToUtc == null || item.ValidToUtc > now)
                && (buildingId == null || item.BuildingId == buildingId)
                && (apartmentId == null || item.ApartmentId == apartmentId))
            .OrderByDescending(item => item.IsPrimary)
            .ThenByDescending(item => item.ValidFromUtc)
            .Select(item => new { item.BuildingId, item.ApartmentId })
            .FirstOrDefaultAsync(cancellationToken);

        match.Account.LastLoginAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);

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
