using System.Data;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagementMvc.Services;

/// <summary>Authenticates resident, admin, and allow-listed SuperAdmin accounts against target identity tables.</summary>
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


    /// <summary>Authenticates an allow-listed SuperAdmin against the target identity schema.</summary>
    public async Task<AuthResult> AuthenticateSuperAdminAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            !SuperAdminIdentityPolicy.IsEmailAllowed(normalizedEmail) || string.IsNullOrEmpty(password))
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
            !SuperAdminIdentityPolicy.IsEmailAllowed(normalizedEmail) ||
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
        var activeCredentials = await db.UserCredentials.AsNoTracking()
            .Where(item => item.AccountId == match.Account.AccountId && item.CredentialType == 0
                && item.RevokedAtUtc == null && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
            .Select(item => new { item.CredentialHash, item.HashAlgorithm })
            .ToListAsync(cancellationToken);
        if (activeCredentials.Count > 0)
        {
            var alreadyConfiguredWithSuppliedPassword = activeCredentials.Any(item =>
                (item.HashAlgorithm == "BCrypt.Net-Next" || item.HashAlgorithm == "bcrypt-v1")
                && _hasher.Verify(password, item.CredentialHash));
            await transaction.RollbackAsync(cancellationToken);
            return alreadyConfiguredWithSuppliedPassword;
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
        // Invalidate any previously issued sessions for this account.
        match.Account.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <returns>Null only when no target admin identity owns the phone; a matching target identity always fails closed.</returns>
    public async Task<AuthResult?> AuthenticateAdminAsync(
        string identifier, string credential, string? buildingId = null, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = AuthHelpers.NormalizePhone(identifier);
        if (string.IsNullOrWhiteSpace(normalizedPhone) || credential is null)
            return AuthResult.Fail("wrong-credentials");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        // Resolve mobile identity inside the selected Admin authorization scope.
        // Resident and Admin roles may coexist for one person; Resident-only records
        // sharing a contact phone do not block a unique Admin account in this building.
        // Multiple Admin accounts in the same scope continue to fail closed.
        var candidates = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            join assignment in db.UserRoleAssignments.AsNoTracking() on account.AccountId equals assignment.AccountId
            join definition in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals definition.RoleKey
            where person.PrimaryPhone != null
                && account.AccountStatus == 1
                && !person.IsArchived
                && (assignment.RoleKey == "admin" || assignment.RoleKey == "building-admin")
                && definition.IsActive
                && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                && assignment.RevokedAtUtc == null
                && assignment.ScopeType == 1
                && assignment.ScopeBuildingId != null
                && (buildingId == null || assignment.ScopeBuildingId == buildingId)
            select new { Account = account, Person = person })
            .Distinct()
            .ToListAsync(cancellationToken);
        var matchingCandidates = candidates.Where(candidate => string.Equals(
            AuthHelpers.NormalizePhone(candidate.Person.PrimaryPhone), normalizedPhone, StringComparison.Ordinal))
            .GroupBy(candidate => candidate.Account.AccountId)
            .Select(group => group.First())
            .ToList();
        if (matchingCandidates.Count == 0) return null;
        if (matchingCandidates.Count != 1) return AuthResult.Fail("wrong-credentials");

        var match = matchingCandidates[0];
        var hasSuperAdminRole = await db.UserRoleAssignments.AsNoTracking().AnyAsync(
            assignment => assignment.AccountId == match.Account.AccountId
                && assignment.RoleKey == "superadmin" && assignment.RevokedAtUtc == null,
            cancellationToken);
        if (hasSuperAdminRole) return AuthResult.Fail("wrong-credentials");

        var adminAssignments = await (
            from assignment in db.UserRoleAssignments.AsNoTracking()
            join definition in db.RoleDefinitions.AsNoTracking() on assignment.RoleKey equals definition.RoleKey
            where assignment.AccountId == match.Account.AccountId && (assignment.RoleKey == "admin" || assignment.RoleKey == "building-admin")
                && definition.IsActive && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                && assignment.RevokedAtUtc == null && assignment.ScopeType == 1
                && assignment.ScopeBuildingId != null
            select assignment.ScopeBuildingId!).Distinct().ToListAsync(cancellationToken);
        if (adminAssignments.Count == 0) return null;
        if (buildingId != null && !adminAssignments.Contains(buildingId, StringComparer.Ordinal))
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

        var scopedBuildings = buildingId == null ? adminAssignments : new List<string> { buildingId };
        var basePermissions = await db.RolePermissions.AsNoTracking()
            .Where(item => (item.RoleKey == "admin" || item.RoleKey == "building-admin") && item.Effect == 1)
            .Select(item => item.PermissionKey).Distinct().ToListAsync(cancellationToken);
        var permissionOverrides = await db.AccountPermissionOverrides.AsNoTracking()
            .Where(item => item.AccountId == match.Account.AccountId && item.ScopeType == 1
                && item.ScopeBuildingId != null && scopedBuildings.Contains(item.ScopeBuildingId)
                && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)
                && item.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        var effectivePerBuilding = scopedBuildings.Select(scopeBuildingId =>
        {
            var scoped = permissionOverrides.Where(item => item.ScopeBuildingId == scopeBuildingId).ToList();
            return basePermissions.Where(permission => !scoped.Any(item =>
                    item.PermissionKey == permission && item.Effect == 0))
                .Concat(scoped.Where(item => item.Effect == 1).Select(item => item.PermissionKey))
                .Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        }).ToList();
        var permissions = effectivePerBuilding.Skip(1).Aggregate(effectivePerBuilding[0], (current, next) =>
        {
            current.IntersectWith(next);
            return current;
        }).OrderBy(value => value, StringComparer.Ordinal).ToList();

        match.Account.LastLoginAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);

        var result = AuthResult.Ok(
            uid: match.Account.AccountId.ToString("D"),
            role: "admin",
            email: match.Account.NormalizedEmail ?? match.Person.PrimaryEmail ?? "",
            name: match.Person.DisplayName,
            buildingIds: buildingId == null ? adminAssignments : new List<string> { buildingId },
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

    /// <summary>Apartment-wide PIN reset is disabled for individual resident accounts.</summary>
    public Task<int> ResetApartmentResidentPinsAsync(
        string buildingId, string apartmentId, string newPin, string actorReference,
        CancellationToken cancellationToken = default)
    {
        // An apartment QR link cannot prove which individual resident is requesting a
        // credential change. Each resident PIN must be reset against that person's account.
        return Task.FromResult(0);
    }
}
