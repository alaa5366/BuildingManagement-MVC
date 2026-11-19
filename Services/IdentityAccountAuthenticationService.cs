using System.Data;
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
        var phone = identifier.Contains('@')
            ? ""
            : AuthHelpers.NormalizePhone(identifier);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        if (!string.IsNullOrEmpty(phone))
        {
            var phonePeople = await db.Persons
                .Where(person => !person.IsArchived && person.PrimaryPhone != null)
                .Select(person => new { person.PersonId, person.PrimaryPhone })
                .ToListAsync(cancellationToken);
            var phonePersonMatches = phonePeople.Count(person =>
                string.Equals(AuthHelpers.NormalizePhone(person.PrimaryPhone), phone, StringComparison.Ordinal));
            if (phonePersonMatches > 1)
                return AuthResult.Fail("wrong-credentials");
        }

        var matches = await (
            from account in db.UserAccounts
            join person in db.Persons on account.PersonId equals person.PersonId
            where account.NormalizedEmail == email
            select new { Account = account, Person = person })
            .Take(2)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrEmpty(phone))
        {
            var phoneCandidates = await (
                from account in db.UserAccounts
                join person in db.Persons on account.PersonId equals person.PersonId
                where person.PrimaryPhone != null
                select new { Account = account, Person = person })
                .ToListAsync(cancellationToken);
            foreach (var candidate in phoneCandidates.Where(item =>
                string.Equals(AuthHelpers.NormalizePhone(item.Person.PrimaryPhone), phone, StringComparison.Ordinal)))
            {
                if (!matches.Any(item => item.Account.AccountId == candidate.Account.AccountId))
                    matches.Add(candidate);
            }
        }

        if (matches.Count == 0 && string.Equals(credential, "1234", StringComparison.Ordinal))
        {
            // First-time resident activation is deliberately limited to one unique,
            // active Person.PrimaryPhone with at least one current occupancy.
            // WhatsApp is not an authentication identifier and no legacy account is modified.
            var provisioned = await ProvisionResidentAccountAsync(db, identifier, cancellationToken);
            if (provisioned)
            {
                matches = await (
                    from account in db.UserAccounts
                    join person in db.Persons on account.PersonId equals person.PersonId
                    where account.NormalizedEmail == email
                    select new { Account = account, Person = person })
                    .Take(2)
                    .ToListAsync(cancellationToken);

                if (!string.IsNullOrEmpty(phone))
                {
                    var phoneCandidates = await (
                        from account in db.UserAccounts
                        join person in db.Persons on account.PersonId equals person.PersonId
                        where person.PrimaryPhone != null
                        select new { Account = account, Person = person })
                        .ToListAsync(cancellationToken);
                    foreach (var candidate in phoneCandidates.Where(item =>
                        string.Equals(AuthHelpers.NormalizePhone(item.Person.PrimaryPhone), phone, StringComparison.Ordinal)))
                    {
                        if (!matches.Any(item => item.Account.AccountId == candidate.Account.AccountId))
                            matches.Add(candidate);
                    }
                }
            }
        }

        if (matches.Count == 0) return null;
        if (matches.Count != 1) return AuthResult.Fail("wrong-credentials");

        var match = matches[0];
        if (match.Account.AccountStatus != 1 || match.Person.IsArchived)
            return AuthResult.Fail("wrong-credentials");

        var now = DateTime.UtcNow;

        // Do not let a newer expired/revoked credential shadow an older still-valid
        // credential. Credential material is opaque: the submitted value is never trimmed.
        var credentials = await db.UserCredentials.AsNoTracking()
            .Where(item => item.AccountId == match.Account.AccountId
                && item.CredentialType == 0
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

        if (occupancy == null)
            return AuthResult.Fail("wrong-credentials");

        var roleExists = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
            where assignment.AccountId == match.Account.AccountId
                  && assignment.RoleKey == "resident"
                  && role.IsActive
                  && assignment.ValidFromUtc <= now
                  && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                  && assignment.RevokedAtUtc == null
                  && (
                      (assignment.ScopeType == 1 && assignment.ScopeBuildingId == occupancy.BuildingId
                          && assignment.ScopeApartmentId == null) ||
                      (assignment.ScopeType == 2 && assignment.ScopeBuildingId == occupancy.BuildingId
                          && assignment.ScopeApartmentId == occupancy.ApartmentId))
            select assignment.RoleAssignmentId)
            .AnyAsync(cancellationToken);
        // Only a building- or apartment-scoped resident assignment can authorize
        // the resolved occupancy. Unscoped/system/personal grants are not resident access.
        if (!roleExists)
            return AuthResult.Fail("wrong-credentials");

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
        result.MustChangePin = match.Account.MustChangePin;
        return result;
    }

    public async Task<bool> ChangeResidentPinAsync(
        Guid accountId,
        string expectedSecurityStamp,
        string newPin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(newPin) || newPin.Length < 4 || newPin.Length > 12 || newPin.Any(character => character < '0' || character > '9')
            || string.Equals(newPin, "1234", StringComparison.Ordinal))
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var account = await db.UserAccounts.SingleOrDefaultAsync(
            item => item.AccountId == accountId, cancellationToken);
        if (account is null || !account.MustChangePin ||
            !string.Equals(account.SecurityStamp, expectedSecurityStamp, StringComparison.Ordinal) ||
            account.AccountStatus != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var now = DateTime.UtcNow;
        var currentCredentials = await db.UserCredentials
            .Where(item => item.AccountId == accountId && item.CredentialType == 0
                && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        var replacementId = Guid.NewGuid();
        db.UserCredentials.Add(new UserCredentialEntity
        {
            CredentialId = replacementId,
            AccountId = accountId,
            CredentialType = 0,
            CredentialHash = _hasher.HashPin(newPin),
            HashAlgorithm = "BCrypt.Net-Next",
            CreatedAtUtc = now
        });
        foreach (var credential in currentCredentials)
        {
            credential.RevokedAtUtc = now;
            credential.ReplacedByCredentialId = replacementId;
        }

        account.MustChangePin = false;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> ProvisionResidentAccountAsync(
        AppDbContext db,
        string identifier,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = AuthHelpers.NormalizePhone(identifier);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
            return false;

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            var people = await db.Persons
                .Where(person => !person.IsArchived && person.PrimaryPhone != null)
                .ToListAsync(cancellationToken);
            var matches = people
                .Where(person => string.Equals(
                    AuthHelpers.NormalizePhone(person.PrimaryPhone),
                    normalizedPhone,
                    StringComparison.Ordinal))
                .ToList();

            // Ambiguous or missing phone numbers are never auto-linked.
            if (matches.Count != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var person = matches[0];
            if (await db.UserAccounts.AnyAsync(
                    account => account.PersonId == person.PersonId,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var now = DateTime.UtcNow;
            var occupancies = await db.ApartmentOccupancies
                .Where(item => item.PersonId == person.PersonId
                    && item.ValidFromUtc <= now
                    && (item.ValidToUtc == null || item.ValidToUtc > now))
                .Select(item => new { item.BuildingId, item.ApartmentId })
                .Distinct()
                .ToListAsync(cancellationToken);

            var residentRoleActive = await db.RoleDefinitions.AnyAsync(
                role => role.RoleKey == "resident" && role.IsActive,
                cancellationToken);
            if (occupancies.Count == 0 || !residentRoleActive)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var accountId = Guid.NewGuid();
            var account = new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = person.PersonId,
                AccountStatus = 1,
                MustChangePin = true,
                NormalizedEmail = string.IsNullOrWhiteSpace(person.PrimaryEmail)
                    ? null
                    : person.PrimaryEmail.Trim().ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = now
            };
            db.UserAccounts.Add(account);
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(),
                AccountId = accountId,
                CredentialType = 0,
                CredentialHash = _hasher.HashPin("1234"),
                HashAlgorithm = "BCrypt.Net-Next",
                CreatedAtUtc = now
            });

            foreach (var occupancy in occupancies)
            {
                db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(),
                    AccountId = accountId,
                    RoleKey = "resident",
                    ScopeType = 2,
                    ScopeBuildingId = occupancy.BuildingId,
                    ScopeApartmentId = occupancy.ApartmentId,
                    ValidFromUtc = now,
                    GrantedAtUtc = now,
                    GrantReason = "Initial resident account activation from verified active occupancy."
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
