using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Creates and manages administrator accounts using only canonical target identity tables.</summary>
public sealed class AdminManagementService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IPasswordHasher _hasher;
    private readonly SessionRevalidator? _sessionRevalidator;

    public AdminManagementService(
        IDbContextFactory<AppDbContext> factory,
        IPasswordHasher hasher,
        SessionRevalidator? sessionRevalidator = null)
    {
        _factory = factory;
        _hasher = hasher;
        _sessionRevalidator = sessionRevalidator;
    }

    /// <summary> Gets or previews data through GetAdminsAsync. </summary>
    public async Task<List<AppUserDoc>> GetAdminsAsync(string? buildingId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var rows = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            join assignment in db.UserRoleAssignments.AsNoTracking() on account.AccountId equals assignment.AccountId
            where (assignment.RoleKey == "admin" || assignment.RoleKey == "building-admin") && assignment.ScopeType == 1
                && (assignment.RevokedAtUtc == null || assignment.RevocationReason == "Administrator account deactivated")
                && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                && (buildingId == null || assignment.ScopeBuildingId == buildingId)
            select new
            {
                Account = account, Person = person, assignment.ScopeBuildingId,
                assignment.RevokedAtUtc, assignment.ValidFromUtc, assignment.ValidToUtc
            })
            .ToListAsync();

        var result = new List<AppUserDoc>();
        foreach (var group in rows.GroupBy(x => x.Account.AccountId))
        {
            var first = group.First();
            var buildings = group.Select(x => x.ScopeBuildingId).Where(x => x != null)
                .Cast<string>().Distinct(StringComparer.Ordinal).ToList();
            var activeBuildings = group.Where(x => x.RevokedAtUtc == null
                    && x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now))
                .Select(x => x.ScopeBuildingId).Where(x => x != null).Cast<string>()
                .Distinct(StringComparer.Ordinal).ToList();
            var permissions = await GetEffectivePermissionsAsync(db, first.Account.AccountId, activeBuildings, now);
            var adminActive = first.Account.AccountStatus == 1 && activeBuildings.Count > 0;
            result.Add(Map(first.Account, first.Person, buildings, permissions, adminActive));
        }
        return result.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary> Gets or previews data through GetAdminAsync. </summary>
    public async Task<AppUserDoc?> GetAdminAsync(string uid)
    {
        if (!Guid.TryParse(uid, out var accountId)) return null;
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var record = await (
            from account in db.UserAccounts.AsNoTracking()
            join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
            where account.AccountId == accountId
            select new { Account = account, Person = person }).SingleOrDefaultAsync();
        if (record is null) return null;

        var assignments = await db.UserRoleAssignments.AsNoTracking()
            .Where(item => item.AccountId == accountId && (item.RoleKey == "admin" || item.RoleKey == "building-admin")
                && item.ScopeType == 1
                && (item.RevokedAtUtc == null || item.RevocationReason == "Administrator account deactivated")
                && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now))
            .ToListAsync();
        if (assignments.Count == 0) return null;
        var buildings = assignments.Select(item => item.ScopeBuildingId!).Distinct().ToList();
        var activeBuildings = assignments.Where(item => item.RevokedAtUtc == null)
            .Select(item => item.ScopeBuildingId!).Distinct().ToList();
        var permissions = await GetEffectivePermissionsAsync(db, accountId, activeBuildings, now);
        var adminActive = record.Account.AccountStatus == 1 && activeBuildings.Count > 0;
        return Map(record.Account, record.Person, buildings, permissions, adminActive);
    }

    /// <summary> Creates or saves data through CreateAdminAsync. </summary>
    public async Task<string> CreateAdminAsync(
        string name, string phone, string buildingId, string pin,
        List<string> permissions, string createdBy)
    {
        var displayName = (name ?? "").Trim();
        var normalizedPhone = AuthHelpers.NormalizePhone(phone);
        if (displayName.Length is < 2 or > 160 || string.IsNullOrWhiteSpace(normalizedPhone))
            throw new ArgumentException("A valid administrator name and phone number are required.");
        if (!IsValidPin(pin)) throw new ArgumentException("PIN must contain 4 to 12 digits.");
        if (string.IsNullOrWhiteSpace(buildingId) || buildingId.Length > 64)
            throw new ArgumentException("A valid building is required.");

        var now = DateTime.UtcNow;
        var grantedBy = Guid.TryParse(createdBy, out var actorId) ? actorId : (Guid?)null;
        var selectedPermissions = NormalizePermissions(permissions);

        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var buildingExists = await db.Buildings.AnyAsync(item => item.Id == buildingId);
        if (!buildingExists) throw new InvalidOperationException("The selected building does not exist.");

        // A shared phone reuses the one canonical person/account so a resident can
        // also administer the building without creating a second login identity.
        var superAdminPersonIds = await (
            from userAccountRow in db.UserAccounts.AsNoTracking()
            join superAdminAssignment in db.UserRoleAssignments.AsNoTracking()
                on userAccountRow.AccountId equals superAdminAssignment.AccountId
            where superAdminAssignment.RoleKey == "superadmin" && superAdminAssignment.RevokedAtUtc == null
            select userAccountRow.PersonId).Distinct().ToListAsync();
        var phonePeople = await db.Persons.Where(item => !item.IsArchived && item.PrimaryPhone != null
            && !superAdminPersonIds.Contains(item.PersonId)).ToListAsync();
        var matchingPeople = phonePeople.Where(item => string.Equals(
            AuthHelpers.NormalizePhone(item.PrimaryPhone), normalizedPhone, StringComparison.Ordinal)).ToList();
        PersonEntity? person = matchingPeople.Count == 1 ? matchingPeople[0] : null;
        if (matchingPeople.Count > 1)
        {
            var matchingPersonIds = matchingPeople.Select(item => item.PersonId).ToHashSet();
            var matchingAccounts = await db.UserAccounts.AsNoTracking()
                .Where(item => matchingPersonIds.Contains(item.PersonId)).ToListAsync();
            var matchingAccountIds = matchingAccounts.Select(item => item.AccountId).ToHashSet();
            var activeAssignments = await db.UserRoleAssignments.AsNoTracking()
                .Where(item => matchingAccountIds.Contains(item.AccountId) && item.RevokedAtUtc == null
                    && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now))
                .ToListAsync();
            var credentials = await db.UserCredentials.AsNoTracking()
                .Where(item => matchingAccountIds.Contains(item.AccountId) && item.CredentialType == 0
                    && item.RevokedAtUtc == null && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
                .ToListAsync();
            person = matchingPeople
                .OrderByDescending(candidate => matchingAccounts.Any(item => item.PersonId == candidate.PersonId))
                .ThenByDescending(candidate => activeAssignments.Count(item =>
                    matchingAccounts.Any(account => account.PersonId == candidate.PersonId && account.AccountId == item.AccountId)
                    && item.RoleKey == "resident" && item.ScopeType == 2))
                .ThenByDescending(candidate => activeAssignments.Count(item =>
                    matchingAccounts.Any(account => account.PersonId == candidate.PersonId && account.AccountId == item.AccountId)
                    && (item.RoleKey == "admin" || item.RoleKey == "building-admin")))
                .ThenByDescending(candidate => matchingAccounts.Any(item =>
                    item.PersonId == candidate.PersonId && item.AccountStatus == 1))
                .ThenByDescending(candidate => credentials.Count(item =>
                    matchingAccounts.Any(account => account.PersonId == candidate.PersonId && account.AccountId == item.AccountId)))
                .ThenBy(candidate => candidate.CreatedAtUtc)
                .ThenBy(candidate => candidate.PersonId)
                .First();
        }
        var personCreated = person is null;
        if (person is null)
        {
            person = new PersonEntity
            {
                PersonId = Guid.NewGuid(),
                DisplayName = displayName,
                PrimaryPhone = normalizedPhone,
                Whatsapp = normalizedPhone,
                CreatedAtUtc = now
            };
            db.Persons.Add(person);
        }

        var account = await db.UserAccounts.SingleOrDefaultAsync(item => item.PersonId == person.PersonId);
        var accountCreated = account is null;
        if (account is null)
        {
            account = new UserAccountEntity
            {
                AccountId = Guid.NewGuid(),
                PersonId = person.PersonId,
                AccountStatus = 1,
                MustChangePin = false,
                NormalizedEmail = AuthHelpers.AdminEmailForPhone(buildingId, normalizedPhone).ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = now
            };
            db.UserAccounts.Add(account);
        }
        else if (account.AccountStatus is 2 or 3)
        {
            throw new InvalidOperationException("admin-existing-account-disabled");
        }

        var activeCredentialExists = await db.UserCredentials.AnyAsync(item =>
            item.AccountId == account.AccountId && item.CredentialType == 0
            && item.RevokedAtUtc == null && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now));
        var credentialCreated = false;
        if (!activeCredentialExists)
        {
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(),
                AccountId = account.AccountId,
                CredentialType = 0,
                CredentialHash = _hasher.HashPin(pin),
                HashAlgorithm = "BCrypt.Net-Next",
                CreatedAtUtc = now
            });
            account.MustChangePin = false;
            credentialCreated = true;
        }
        account.AccountStatus = 1;
        account.NormalizedEmail ??= AuthHelpers.AdminEmailForPhone(buildingId, normalizedPhone).ToUpperInvariant();
        account.SecurityStamp = Guid.NewGuid().ToString("N");

        var adminRole = await db.RoleDefinitions.SingleOrDefaultAsync(item => item.RoleKey == "building-admin");
        if (adminRole is null)
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "building-admin", DisplayName = "Building Administrator",
                IsSystemRole = true, IsActive = true
            });
        else if (!adminRole.IsActive)
            throw new InvalidOperationException("building-admin-role-inactive");

        // If this person already occupies apartments but did not yet have a login,
        // provision the Resident scopes on the same account instead of creating an
        // Admin-only identity for someone who is also a resident.
        var activeOccupancies = await db.ApartmentOccupancies.Where(item =>
            item.PersonId == person.PersonId && item.ValidFromUtc <= now
            && (item.ValidToUtc == null || item.ValidToUtc > now)).ToListAsync();
        if (activeOccupancies.Count > 0)
        {
            var residentRole = await db.RoleDefinitions.SingleOrDefaultAsync(item => item.RoleKey == "resident");
            if (residentRole is null)
                db.RoleDefinitions.Add(new RoleDefinitionEntity
                {
                    RoleKey = "resident", DisplayName = "Resident", IsSystemRole = true, IsActive = true
                });
            else if (!residentRole.IsActive)
                throw new InvalidOperationException("resident-role-inactive");

            foreach (var occupancy in activeOccupancies)
            {
                var residentScopeExists = await db.UserRoleAssignments.AnyAsync(item =>
                    item.AccountId == account.AccountId && item.RoleKey == "resident" && item.ScopeType == 2
                    && item.ScopeBuildingId == occupancy.BuildingId && item.ScopeApartmentId == occupancy.ApartmentId
                    && item.RevokedAtUtc == null && item.ValidFromUtc <= now
                    && (item.ValidToUtc == null || item.ValidToUtc > now));
                if (!residentScopeExists)
                {
                    db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
                    {
                        RoleAssignmentId = Guid.NewGuid(), AccountId = account.AccountId,
                        RoleKey = "resident", ScopeType = 2,
                        ScopeBuildingId = occupancy.BuildingId, ScopeApartmentId = occupancy.ApartmentId,
                        ValidFromUtc = now, GrantedAtUtc = now, GrantedByAccountId = grantedBy,
                        GrantReason = "Resident access linked to the shared account during Admin activation"
                    });
                }
            }
        }

        var assignmentExists = await db.UserRoleAssignments.AnyAsync(item =>
            item.AccountId == account.AccountId && item.RoleKey == "building-admin"
            && item.ScopeType == 1 && item.ScopeBuildingId == buildingId
            && item.RevokedAtUtc == null && item.ValidFromUtc <= now
            && (item.ValidToUtc == null || item.ValidToUtc > now));
        if (!assignmentExists)
        {
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(),
                AccountId = account.AccountId,
                RoleKey = "building-admin",
                ScopeType = 1,
                ScopeBuildingId = buildingId,
                ValidFromUtc = now,
                GrantedAtUtc = now,
                GrantedByAccountId = grantedBy,
                GrantReason = "Administrator access granted"
            });
        }
        AddPermissionOverrides(db, account.AccountId, new[] { buildingId }, selectedPermissions, grantedBy, now);
        db.AuditEvents.Add(CreateAudit(grantedBy, createdBy, buildingId, "admin.create",
            account.AccountId.ToString("D"), new
            {
                accountId = account.AccountId, personId = person.PersonId, personCreated, accountCreated,
                credentialCreated, reusedExistingIdentity = !personCreated,
                phone = normalizedPhone, permissions = selectedPermissions
            }, now));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return account.AccountId.ToString("D");
    }

    /// <summary> Updates account or operational data through UpdateAdminAsync. </summary>
    public async Task UpdateAdminAsync(
        string uid, string name, string phone, List<string> permissions, bool isActive, string updatedBy)
    {
        if (!Guid.TryParse(uid, out var accountId)) throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));
        var displayName = (name ?? "").Trim();
        var normalizedPhone = AuthHelpers.NormalizePhone(phone);
        if (displayName.Length is < 2 or > 160 || string.IsNullOrWhiteSpace(normalizedPhone))
            throw new ArgumentException("A valid administrator name and phone number are required.");

        var now = DateTime.UtcNow;
        var grantedBy = Guid.TryParse(updatedBy, out var actorId) ? actorId : (Guid?)null;
        var selectedPermissions = NormalizePermissions(permissions);
        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var account = await db.UserAccounts.SingleOrDefaultAsync(item => item.AccountId == accountId);
        var person = account is null ? null : await db.Persons.SingleOrDefaultAsync(item => item.PersonId == account.PersonId);
        if (account is null || person is null) throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));

        var assignments = await db.UserRoleAssignments.Where(item => item.AccountId == accountId
            && (item.RoleKey == "admin" || item.RoleKey == "building-admin") && item.ScopeType == 1
            && (item.RevokedAtUtc == null || item.RevocationReason == "Administrator account deactivated")
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)).ToListAsync();
        if (assignments.Count == 0) throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));

        // A shared phone is accepted during role assignment (CreateAdminAsync reuses
        // the canonical identity). A profile change cannot silently merge two existing people.
        var superAdminPersonIdsForPhoneCheck = await (
            from userAccountRow in db.UserAccounts.AsNoTracking()
            join superAdminAssignment in db.UserRoleAssignments.AsNoTracking()
                on userAccountRow.AccountId equals superAdminAssignment.AccountId
            where superAdminAssignment.RoleKey == "superadmin" && superAdminAssignment.RevokedAtUtc == null
            select userAccountRow.PersonId).Distinct().ToListAsync();
        var phoneOwners = await db.Persons.AsNoTracking()
            .Where(item => item.PersonId != person.PersonId && !item.IsArchived
                && item.PrimaryPhone != null && !superAdminPersonIdsForPhoneCheck.Contains(item.PersonId))
            .Select(item => item.PrimaryPhone).ToListAsync();
        if (phoneOwners.Any(value => string.Equals(
            AuthHelpers.NormalizePhone(value), normalizedPhone, StringComparison.Ordinal)))
            throw new InvalidOperationException("admin-phone-change-requires-identity-resolution");

        person.DisplayName = displayName;
        person.PrimaryPhone = normalizedPhone;
        person.Whatsapp = normalizedPhone;
        person.UpdatedAtUtc = now;
        foreach (var assignment in assignments)
        {
            if (isActive && assignment.RevokedAtUtc != null
                && assignment.RevocationReason == "Administrator account deactivated")
            {
                assignment.RevokedAtUtc = null;
                assignment.RevokedByAccountId = null;
                assignment.RevocationReason = null;
            }
            else if (!isActive && assignment.RevokedAtUtc == null)
            {
                assignment.RevokedAtUtc = now;
                assignment.RevokedByAccountId = grantedBy;
                assignment.RevocationReason = "Administrator account deactivated";
            }
        }
        var hasOtherActiveRole = await db.UserRoleAssignments.AnyAsync(item =>
            item.AccountId == accountId
            && item.RoleKey != "admin" && item.RoleKey != "building-admin"
            && item.RevokedAtUtc == null && item.ValidFromUtc <= now
            && (item.ValidToUtc == null || item.ValidToUtc > now));
        var hasActiveAdminRole = isActive;
        account.AccountStatus = hasActiveAdminRole || hasOtherActiveRole ? (byte)1 : (byte)0;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        var buildings = assignments.Select(item => item.ScopeBuildingId!).Distinct(StringComparer.Ordinal).ToArray();
        AddPermissionOverrides(db, accountId, buildings, selectedPermissions, grantedBy, now);
        db.AuditEvents.Add(CreateAudit(grantedBy, updatedBy, buildings.FirstOrDefault(), "admin.update",
            accountId.ToString("D"), new { accountId, isActive, permissions = selectedPermissions }, now));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        _sessionRevalidator?.InvalidateAdmin(uid);
    }

    /// <summary> Updates account or operational data through ChangePinAsync. </summary>
    public async Task ChangePinAsync(string uid, string newPin, string updatedBy)
    {
        if (!Guid.TryParse(uid, out var accountId) || !IsValidPin(newPin))
            throw new ArgumentException("A valid account and 4 to 12 digit PIN are required.");
        var now = DateTime.UtcNow;
        await using var db = await _factory.CreateDbContextAsync();
        var account = await db.UserAccounts.SingleOrDefaultAsync(item => item.AccountId == accountId)
            ?? throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));
        var oldCredentials = await db.UserCredentials.Where(item => item.AccountId == accountId
            && item.CredentialType == 0 && item.RevokedAtUtc == null).ToListAsync();
        foreach (var old in oldCredentials) old.RevokedAtUtc = now;
        db.UserCredentials.Add(new UserCredentialEntity
        {
            CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0,
            CredentialHash = _hasher.HashPin(newPin), HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
        });
        account.MustChangePin = false;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        db.AuditEvents.Add(CreateAudit(Guid.TryParse(updatedBy, out var actorId) ? actorId : null,
            updatedBy, null, "admin.change_pin", accountId.ToString("D"), new { accountId }, now));
        await db.SaveChangesAsync();
        _sessionRevalidator?.InvalidateAdmin(uid);
    }

    /// <summary> Performs the DeactivateAsync operation. </summary>
    public Task DeactivateAsync(string uid, string updatedBy) => SetStatusAsync(uid, false, updatedBy);
    /// <summary> Performs the ActivateAsync operation. </summary>
    public Task ActivateAsync(string uid, string updatedBy) => SetStatusAsync(uid, true, updatedBy);

    private async Task SetStatusAsync(string uid, bool activate, string updatedBy)
    {
        if (!Guid.TryParse(uid, out var accountId)) throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));
        var now = DateTime.UtcNow;
        await using var db = await _factory.CreateDbContextAsync();
        var account = await db.UserAccounts.SingleOrDefaultAsync(item => item.AccountId == accountId)
            ?? throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));
        var assignments = await db.UserRoleAssignments.Where(item => item.AccountId == accountId
            && (item.RoleKey == "admin" || item.RoleKey == "building-admin") && item.ScopeType == 1
            && (item.RevokedAtUtc == null || item.RevocationReason == "Administrator account deactivated")
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)).ToListAsync();
        if (assignments.Count == 0)
            throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));
        var actorId = Guid.TryParse(updatedBy, out var parsedActor) ? parsedActor : (Guid?)null;
        foreach (var assignment in assignments)
        {
            if (activate && assignment.RevokedAtUtc != null)
            {
                assignment.RevokedAtUtc = null;
                assignment.RevokedByAccountId = null;
                assignment.RevocationReason = null;
            }
            else if (!activate && assignment.RevokedAtUtc == null)
            {
                assignment.RevokedAtUtc = now;
                assignment.RevokedByAccountId = actorId;
                assignment.RevocationReason = "Administrator account deactivated";
            }
        }
        var hasOtherActiveRole = await db.UserRoleAssignments.AnyAsync(item => item.AccountId == accountId
            && item.RoleKey != "admin" && item.RoleKey != "building-admin"
            && item.RevokedAtUtc == null && item.ValidFromUtc <= now
            && (item.ValidToUtc == null || item.ValidToUtc > now));
        account.AccountStatus = activate || hasOtherActiveRole ? (byte)1 : (byte)0;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        db.AuditEvents.Add(CreateAudit(actorId,
            updatedBy, null, activate ? "admin.activate" : "admin.deactivate",
            accountId.ToString("D"), new { accountId }, now));
        await db.SaveChangesAsync();
        _sessionRevalidator?.InvalidateAdmin(uid);
    }

    /// <summary> Removes or revokes data through DeleteAsync. </summary>
    public async Task DeleteAsync(string uid, string deletedBy)
    {
        if (!Guid.TryParse(uid, out var accountId)) throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));
        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(deletedBy, out var parsedActor) ? parsedActor : (Guid?)null;
        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var account = await db.UserAccounts.SingleOrDefaultAsync(item => item.AccountId == accountId)
            ?? throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));
        var assignments = await db.UserRoleAssignments.Where(item => item.AccountId == accountId
            && (item.RoleKey == "admin" || item.RoleKey == "building-admin") && item.RevokedAtUtc == null).ToListAsync();
        if (assignments.Count == 0) throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));
        foreach (var assignment in assignments)
        {
            assignment.RevokedAtUtc = now;
            assignment.RevokedByAccountId = actorId;
            assignment.RevocationReason = "Administrator access removed";
        }
        var hasOtherActiveRole = await db.UserRoleAssignments.AnyAsync(item => item.AccountId == accountId
            && item.RoleKey != "admin" && item.RoleKey != "building-admin" && item.RevokedAtUtc == null
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now));
        if (!hasOtherActiveRole) account.AccountStatus = 0;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        db.AuditEvents.Add(CreateAudit(actorId, deletedBy, null, "admin.delete",
            accountId.ToString("D"), new { accountId, accessRevoked = true, hardDelete = false }, now));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        _sessionRevalidator?.InvalidateAdmin(uid);
    }

    /// <summary> Updates account or operational data through SyncPermissionsAsync. </summary>
    public async Task SyncPermissionsAsync(string uid, List<string> permissions, string updatedBy)
    {
        if (!Guid.TryParse(uid, out var accountId)) return;
        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(updatedBy, out var parsedActor) ? parsedActor : (Guid?)null;
        await using var db = await _factory.CreateDbContextAsync();
        var buildings = await db.UserRoleAssignments.Where(item => item.AccountId == accountId
            && (item.RoleKey == "admin" || item.RoleKey == "building-admin") && item.RevokedAtUtc == null && item.ScopeBuildingId != null)
            .Select(item => item.ScopeBuildingId!).Distinct().ToArrayAsync();
        if (buildings.Length == 0) return;
        AddPermissionOverrides(db, accountId, buildings, NormalizePermissions(permissions), actorId, now);
        db.AuditEvents.Add(CreateAudit(actorId, updatedBy, buildings.FirstOrDefault(), "admin.sync_permissions",
            accountId.ToString("D"), new { accountId, count = permissions?.Count ?? 0 }, now));
        await db.SaveChangesAsync();
        _sessionRevalidator?.InvalidateAdmin(uid);
    }

    private static AppUserDoc Map(UserAccountEntity account, PersonEntity person,
        List<string> buildings, List<string> permissions, bool adminActive) => new()
    {
        Uid = account.AccountId.ToString("D"),
        Email = account.NormalizedEmail ?? person.PrimaryEmail ?? "",
        Name = person.DisplayName,
        Phone = person.PrimaryPhone ?? "",
        Whatsapp = person.Whatsapp ?? "",
        PhotoUrl = person.PhotoUrl ?? "",
        Role = "admin",
        BuildingIds = buildings,
        Disabled = !adminActive,
        IsActive = adminActive,
        Permissions = permissions,
        LastLoginAt = account.LastLoginAtUtc?.ToString("o"),
        CreatedAt = account.CreatedAtUtc.ToString("o")
    };

    private static List<string> NormalizePermissions(IEnumerable<string>? permissions) =>
        (permissions ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()).Distinct(StringComparer.Ordinal).ToList();

    private static bool IsValidPin(string? pin) =>
        !string.IsNullOrWhiteSpace(pin) && pin.Length is >= 4 and <= 12 && pin.All(char.IsDigit);

    private static void AddPermissionOverrides(AppDbContext db, Guid accountId,
        IEnumerable<string> buildingIds, IReadOnlyCollection<string> selectedPermissions,
        Guid? actorId, DateTime now)
    {
        var buildings = buildingIds.Distinct(StringComparer.Ordinal).ToArray();
        var permissionKeys = AdminPermissions.All.Distinct(StringComparer.Ordinal).ToArray();
        var old = db.AccountPermissionOverrides.Where(item => item.AccountId == accountId
            && item.ScopeType == 1 && item.ScopeBuildingId != null
            && buildings.Contains(item.ScopeBuildingId) && permissionKeys.Contains(item.PermissionKey)
            && item.RevokedAtUtc == null).ToList();
        foreach (var item in old)
        {
            item.RevokedAtUtc = now;
            item.RevokedByAccountId = actorId;
            item.RevocationReason = "Administrator permission profile replaced";
        }
        foreach (var buildingId in buildings)
        foreach (var permission in permissionKeys)
        {
            db.AccountPermissionOverrides.Add(new AccountPermissionOverrideEntity
            {
                OverrideId = Guid.NewGuid(),
                AccountId = accountId,
                PermissionKey = permission,
                Effect = selectedPermissions.Contains(permission, StringComparer.Ordinal) ? (byte)1 : (byte)0,
                ScopeType = 1,
                ScopeBuildingId = buildingId,
                ValidFromUtc = now,
                GrantedAtUtc = now,
                GrantedByAccountId = actorId,
                GrantReason = "Administrator permission profile"
            });
        }
    }

    private static async Task<List<string>> GetEffectivePermissionsAsync(
        AppDbContext db, Guid accountId, IReadOnlyCollection<string> buildingIds, DateTime now)
    {
        if (buildingIds.Count == 0) return new List<string>();
        var basePermissions = await db.RolePermissions.AsNoTracking()
            .Where(item => (item.RoleKey == "admin" || item.RoleKey == "building-admin") && item.Effect == 1)
            .Select(item => item.PermissionKey).Distinct().ToListAsync();
        var overrides = await db.AccountPermissionOverrides.AsNoTracking()
            .Where(item => item.AccountId == accountId && item.ScopeType == 1
                && item.ScopeBuildingId != null && buildingIds.Contains(item.ScopeBuildingId)
                && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)
                && item.RevokedAtUtc == null)
            .ToListAsync();
        var perBuilding = buildingIds.Select(buildingId =>
        {
            var scoped = overrides.Where(item => item.ScopeBuildingId == buildingId).ToList();
            return basePermissions.Where(permission => !scoped.Any(item =>
                    item.PermissionKey == permission && item.Effect == 0))
                .Concat(scoped.Where(item => item.Effect == 1).Select(item => item.PermissionKey))
                .Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        }).ToList();
        return perBuilding.Skip(1).Aggregate(perBuilding[0], (current, next) =>
        {
            current.IntersectWith(next);
            return current;
        }).OrderBy(value => value, StringComparer.Ordinal).ToList();
    }

    private static AuditEventEntity CreateAudit(Guid? actorAccountId, string? actorReference,
        string? buildingId, string eventType, string resourceReference, object details, DateTime now) => new()
    {
        AuditEventId = Guid.NewGuid(),
        OccurredAtUtc = now,
        ActorAccountId = actorAccountId,
        ActorReference = string.IsNullOrWhiteSpace(actorReference) ? "system" : actorReference,
        BuildingId = buildingId,
        EventCategory = "Administration",
        EventType = eventType,
        ResourceType = "UserAccount",
        ResourceReference = resourceReference,
        Outcome = 0,
        SafeDetailsJson = System.Text.Json.JsonSerializer.Serialize(details)
    };
}
