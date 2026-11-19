using System.Data;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagementMvc.Services;

/// <summary>Authenticates resident accounts against the target identity tables.</summary>
public sealed class IdentityAccountAuthenticationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<IdentityAccountAuthenticationService>? _logger;

    public IdentityAccountAuthenticationService(
        IDbContextFactory<AppDbContext> dbFactory,
        IPasswordHasher hasher,
        ILogger<IdentityAccountAuthenticationService>? logger = null)
    {
        _dbFactory = dbFactory;
        _hasher = hasher;
        _logger = logger;
    }

    /// <summary>
    /// Issues a resident sign-in result only for the active primary occupant and an active target-schema resident account.
    /// This is used after a valid, single-use QR capability has been consumed; no legacy Resident/UID store is consulted.
    /// </summary>
    public async Task<AuthResult> AuthenticateResidentWithQuickQrAsync(
        string buildingId, string apartmentId, bool requirePinChange = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(apartmentId))
            return AuthResult.Fail("wrong-credentials");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var matches = await (
            from occupancy in db.ApartmentOccupancies
            join person in db.Persons on occupancy.PersonId equals person.PersonId
            join account in db.UserAccounts on person.PersonId equals account.PersonId
            where occupancy.BuildingId == buildingId
                && occupancy.ApartmentId == apartmentId
                && occupancy.IsPrimary
                && occupancy.ValidFromUtc <= now
                && (occupancy.ValidToUtc == null || occupancy.ValidToUtc > now)
                && account.AccountStatus == 1
                && !person.IsArchived
            select new { Occupancy = occupancy, Person = person, Account = account })
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matches.Count != 1)
            return AuthResult.Fail("wrong-credentials");

        var match = matches[0];
        if (requirePinChange || match.Account.MustChangePin)
        {
            var normalizedPhone = AuthHelpers.NormalizePhone(match.Person.PrimaryPhone);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
                return AuthResult.Fail("wrong-credentials");
            var peopleWithPhone = await db.Persons.AsNoTracking()
                .Where(person => person.PrimaryPhone != null)
                .Select(person => person.PrimaryPhone)
                .ToListAsync(cancellationToken);
            if (peopleWithPhone.Count(phone => string.Equals(
                AuthHelpers.NormalizePhone(phone), normalizedPhone, StringComparison.Ordinal)) != 1)
                return AuthResult.Fail("wrong-credentials");
        }

        var hasResidentRole = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join role in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals role.RoleKey
            where assignment.AccountId == match.Account.AccountId
                && assignment.RoleKey == "resident"
                && role.IsActive
                && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                && assignment.RevokedAtUtc == null
                && ((assignment.ScopeType == 1 && assignment.ScopeBuildingId == buildingId && assignment.ScopeApartmentId == null)
                    || (assignment.ScopeType == 2 && assignment.ScopeBuildingId == buildingId && assignment.ScopeApartmentId == apartmentId))
            select assignment.RoleAssignmentId)
            .AnyAsync(cancellationToken);
        if (!hasResidentRole)
            return AuthResult.Fail("wrong-credentials");

        match.Account.LastLoginAtUtc = now;
        if (requirePinChange)
        {
            // Recovery invalidates existing sessions and forces the resident to choose a new PIN.
            match.Account.MustChangePin = true;
            match.Account.SecurityStamp = Guid.NewGuid().ToString("N");
        }
        await db.SaveChangesAsync(cancellationToken);

        var result = AuthResult.Ok(
            uid: match.Account.AccountId.ToString("D"),
            role: "resident",
            email: match.Account.NormalizedEmail ?? "",
            name: match.Person.DisplayName,
            buildingIds: new List<string> { buildingId },
            aptId: apartmentId);
        result.UsesTargetIdentity = true;
        result.SecurityStamp = match.Account.SecurityStamp;
        result.MustChangePin = match.Account.MustChangePin;
        result.Phone = match.Person.PrimaryPhone;
        return result;
    }

    /// <summary>Verifies that the apartment contact phone is the primary target Person phone used for QR recovery.</summary>
    public async Task<bool> IsPrimaryResidentPhoneMatchAsync(
        string buildingId, string apartmentId, string phone, CancellationToken cancellationToken = default)
    {
        var normalized = AuthHelpers.NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(normalized)) return false;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var allPhones = await db.Persons.AsNoTracking()
            .Where(person => person.PrimaryPhone != null)
            .Select(person => person.PrimaryPhone)
            .ToListAsync(cancellationToken);
        if (allPhones.Count(value => string.Equals(
            AuthHelpers.NormalizePhone(value), normalized, StringComparison.Ordinal)) != 1)
            return false;

        var phones = await (
            from occupancy in db.ApartmentOccupancies.AsNoTracking()
            join person in db.Persons.AsNoTracking() on occupancy.PersonId equals person.PersonId
            join account in db.UserAccounts.AsNoTracking() on person.PersonId equals account.PersonId
            where occupancy.BuildingId == buildingId
                && occupancy.ApartmentId == apartmentId
                && occupancy.IsPrimary
                && occupancy.ValidFromUtc <= now
                && (occupancy.ValidToUtc == null || occupancy.ValidToUtc > now)
                && account.AccountStatus == 1
                && !person.IsArchived
            select person.PrimaryPhone)
            .Take(2)
            .ToListAsync(cancellationToken);
        return phones.Count == 1 &&
            string.Equals(AuthHelpers.NormalizePhone(phones[0]), normalized, StringComparison.Ordinal);
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
        var targetPersonMatchesPhone = false;
        if (!string.IsNullOrEmpty(phone))
        {
            var phonePeople = await db.Persons
                .Where(person => person.PrimaryPhone != null)
                .Select(person => new { person.PersonId, person.PrimaryPhone })
                .ToListAsync(cancellationToken);
            var phonePersonMatches = phonePeople.Count(person =>
                string.Equals(AuthHelpers.NormalizePhone(person.PrimaryPhone), phone, StringComparison.Ordinal));
            targetPersonMatchesPhone = phonePersonMatches == 1;
            if (phonePersonMatches > 1)
            {
                // Never disclose the duplicated number or matching people in the login response/log.
                _logger?.LogWarning(
                    "Resident sign-in blocked: normalized primary phone matches {MatchCount} person records.",
                    phonePersonMatches);
                return AuthResult.Fail("wrong-credentials");
            }
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

        if (matches.Count == 0 && !string.IsNullOrEmpty(phone) &&
            string.Equals(credential, "1234", StringComparison.Ordinal))
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

        if (matches.Count == 0)
            return targetPersonMatchesPhone
                ? AuthResult.Fail("wrong-credentials")
                : null;
        if (matches.Count != 1) return AuthResult.Fail("wrong-credentials");

        var match = matches[0];
        var hasSuperAdminAssignment = await db.UserRoleAssignments.AsNoTracking().AnyAsync(item =>
            item.AccountId == match.Account.AccountId && item.RoleKey == "superadmin" && item.RevokedAtUtc == null,
            cancellationToken);
        if (hasSuperAdminAssignment || match.Account.AccountStatus != 1 || match.Person.IsArchived)
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


    /// <summary>Authenticates a target-schema admin by the person's primary mobile, PIN, and selected building scope.</summary>
    /// <summary>Authenticates an allow-listed SuperAdmin against the target identity schema.</summary>
    public async Task<AuthResult> AuthenticateSuperAdminAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            !SqlAuthService.IsSuperAdminEmail(normalizedEmail) || string.IsNullOrEmpty(password))
            return AuthResult.Fail("wrong-credentials");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var match = await (
            from account in db.UserAccounts
            join person in db.Persons on account.PersonId equals person.PersonId
            where account.NormalizedEmail == normalizedEmail
            select new { Account = account, Person = person })
            .SingleOrDefaultAsync(cancellationToken);

        if (match is null || match.Account.AccountStatus != 1 || match.Person.IsArchived)
            return AuthResult.Fail("wrong-credentials");

        var now = DateTime.UtcNow;
        var hasGlobalSuperAdminRole = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join definition in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals definition.RoleKey
            where assignment.AccountId == match.Account.AccountId
                && assignment.RoleKey == "superadmin" && definition.IsActive
                && assignment.ScopeType == 0 && assignment.ScopeBuildingId == null
                && assignment.ScopeApartmentId == null && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                && assignment.RevokedAtUtc == null
            select assignment.RoleAssignmentId).AnyAsync(cancellationToken);
        if (!hasGlobalSuperAdminRole) return AuthResult.Fail("wrong-credentials");

        var credentials = await db.UserCredentials.AsNoTracking()
            .Where(item => item.AccountId == match.Account.AccountId && item.CredentialType == 0
                && item.RevokedAtUtc == null && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new { item.CredentialHash, item.HashAlgorithm })
            .Take(8).ToListAsync(cancellationToken);
        var valid = credentials.Any(item =>
            (item.HashAlgorithm == "BCrypt.Net-Next" || item.HashAlgorithm == "bcrypt-v1")
            && _hasher.Verify(password, item.CredentialHash));
        if (!valid) return AuthResult.Fail("wrong-credentials");

        var permissions = await db.RolePermissions.AsNoTracking()
            .Where(item => item.RoleKey == "superadmin" && item.Effect == 1)
            .Select(item => item.PermissionKey).Distinct().ToListAsync(cancellationToken);

        match.Account.LastLoginAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);

        var result = AuthResult.Ok(
            match.Account.AccountId.ToString("D"), "superadmin",
            match.Account.NormalizedEmail ?? normalizedEmail,
            match.Person.DisplayName, new List<string>(), permissions: permissions);
        result.UsesTargetIdentity = true;
        result.SecurityStamp = match.Account.SecurityStamp;
        return result;
    }

    /// <summary>
    /// Explicit one-time bootstrap for an already-existing allow-listed SuperAdmin identity.
    /// It never changes an existing password and stores only a BCrypt hash.
    /// </summary>
    public async Task<bool> BootstrapSuperAdminAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            !SqlAuthService.IsSuperAdminEmail(normalizedEmail) ||
            string.IsNullOrWhiteSpace(password) || password.Length < 14)
            return false;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var match = await (
            from account in db.UserAccounts
            join person in db.Persons on account.PersonId equals person.PersonId
            where account.NormalizedEmail == normalizedEmail
            select new { Account = account, Person = person })
            .SingleOrDefaultAsync(cancellationToken);
        if (match is null || match.Account.AccountStatus != 1 || match.Person.IsArchived)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var now = DateTime.UtcNow;
        var activeCredentialExists = await db.UserCredentials.AnyAsync(item =>
            item.AccountId == match.Account.AccountId && item.CredentialType == 0
            && item.RevokedAtUtc == null && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now),
            cancellationToken);
        if (activeCredentialExists)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var role = await db.RoleDefinitions.SingleOrDefaultAsync(
            item => item.RoleKey == "superadmin", cancellationToken);
        if (role is null)
        {
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "superadmin", DisplayName = "Super Administrator",
                IsSystemRole = true, IsActive = true
            });
        }
        else if (!role.IsActive)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // Grant only code-catalogued permissions that explicitly support global system scope.
        // MFA-required operations remain protected by AuthorizationEvaluator step-up checks.
        var systemPermissions = DefaultAuthorizationPermissionCatalog.SystemGrantablePermissions;
        var existingPermissionKeys = await db.RolePermissions.AsNoTracking()
            .Where(item => item.RoleKey == "superadmin")
            .Select(item => item.PermissionKey).ToListAsync(cancellationToken);
        foreach (var permission in systemPermissions)
        {
            if (!existingPermissionKeys.Contains(permission.Key, StringComparer.Ordinal))
            {
                db.RolePermissions.Add(new RolePermissionEntity
                {
                    RoleKey = "superadmin", PermissionKey = permission.Key, Effect = 1
                });
            }
        }

        var assignmentExists = await db.UserRoleAssignments.AnyAsync(item =>
            item.AccountId == match.Account.AccountId && item.RoleKey == "superadmin"
            && item.ScopeType == 0 && item.ScopeBuildingId == null && item.ScopeApartmentId == null
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)
            && item.RevokedAtUtc == null, cancellationToken);
        if (!assignmentExists)
        {
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = match.Account.AccountId,
                RoleKey = "superadmin", ScopeType = 0, ValidFromUtc = now,
                GrantedAtUtc = now, GrantReason = "Explicit one-time SuperAdmin bootstrap."
            });
        }

        db.UserCredentials.Add(new UserCredentialEntity
        {
            CredentialId = Guid.NewGuid(), AccountId = match.Account.AccountId,
            CredentialType = 0, CredentialHash = _hasher.Hash(password),
            HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <returns>Null only when no target admin identity owns the phone; a matching target identity always fails closed.</returns>
    public async Task<AuthResult?> AuthenticateAdminAsync(
        string identifier, string credential, string buildingId, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = AuthHelpers.NormalizePhone(identifier);
        if (string.IsNullOrWhiteSpace(normalizedPhone) || string.IsNullOrWhiteSpace(buildingId) || credential is null)
            return AuthResult.Fail("wrong-credentials");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var people = await db.Persons.AsNoTracking()
            .Where(person => person.PrimaryPhone != null)
            .Select(person => new { person.PersonId, person.PrimaryPhone })
            .ToListAsync(cancellationToken);
        var matchingPeople = people.Where(person => string.Equals(
            AuthHelpers.NormalizePhone(person.PrimaryPhone), normalizedPhone, StringComparison.Ordinal)).ToList();
        if (matchingPeople.Count == 0) return null;
        if (matchingPeople.Count != 1) return AuthResult.Fail("wrong-credentials");

        var match = await (
            from account in db.UserAccounts
            join person in db.Persons on account.PersonId equals person.PersonId
            where account.PersonId == matchingPeople[0].PersonId
            select new { Account = account, Person = person })
            .SingleOrDefaultAsync(cancellationToken);
        if (match is null) return null;

        var now = DateTime.UtcNow;
        var hasSuperAdminRole = await db.UserRoleAssignments.AsNoTracking().AnyAsync(
            assignment => assignment.AccountId == match.Account.AccountId
                && assignment.RoleKey == "superadmin" && assignment.RevokedAtUtc == null,
            cancellationToken);
        if (hasSuperAdminRole) return AuthResult.Fail("wrong-credentials");

        var adminAssignments = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join definition in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals definition.RoleKey
            where assignment.AccountId == match.Account.AccountId && assignment.RoleKey == "admin"
                && definition.IsActive && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                && assignment.RevokedAtUtc == null && assignment.ScopeType == 1
            select assignment.ScopeBuildingId).Distinct().ToListAsync(cancellationToken);
        if (adminAssignments.Count == 0) return null;
        if (!adminAssignments.Contains(buildingId, StringComparer.Ordinal))
            return AuthResult.Fail("wrong-credentials");
        if (match.Account.AccountStatus != 1 || match.Person.IsArchived)
            return AuthResult.Fail("wrong-credentials");

        var credentials = await db.UserCredentials.AsNoTracking()
            .Where(item => item.AccountId == match.Account.AccountId && item.CredentialType == 0
                && item.RevokedAtUtc == null && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new { item.CredentialHash, item.HashAlgorithm })
            .Take(8)
            .ToListAsync(cancellationToken);
        var valid = credentials.Any(item =>
            (item.HashAlgorithm == "BCrypt.Net-Next" || item.HashAlgorithm == "bcrypt-v1")
            && _hasher.Verify(credential, item.CredentialHash));
        if (!valid) return AuthResult.Fail("wrong-credentials");

        var permissions = await db.RolePermissions.AsNoTracking()
            .Where(item => item.RoleKey == "admin" && item.Effect == 1)
            .Select(item => item.PermissionKey)
            .Distinct()
            .ToListAsync(cancellationToken);

        match.Account.LastLoginAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);

        var result = AuthResult.Ok(
            uid: match.Account.AccountId.ToString("D"),
            role: "admin",
            email: match.Account.NormalizedEmail ?? match.Person.PrimaryEmail ?? "",
            name: match.Person.DisplayName,
            buildingIds: new List<string> { buildingId },
            permissions: permissions);
        result.UsesTargetIdentity = true;
        result.SecurityStamp = match.Account.SecurityStamp;
        result.MustChangePin = match.Account.MustChangePin;
        result.Phone = match.Person.PrimaryPhone;
        return result;
    }

    /// <summary>Changes the PIN for the specified resident account after validating the current credentials.</summary>

    /// <summary>Changes the PIN for the authenticated resident account.</summary>
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
