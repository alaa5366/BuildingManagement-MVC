using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Resident account operations backed exclusively by Persons, UserAccounts, credentials, roles and effective-dated occupancies.</summary>
public sealed class ResidentsService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditLogger _audit;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<ResidentsService> _logger;

    public ResidentsService(
        IDbContextFactory<AppDbContext> factory,
        IAuditLogger audit,
        IPasswordHasher hasher,
        ILogger<ResidentsService> logger)
    {
        _factory = factory;
        _audit = audit;
        _hasher = hasher;
        _logger = logger;
    }

    /// <summary>Lists active people already associated with a building, or all people for a SuperAdmin registration flow.</summary>
    public async Task<List<PersonEntity>> GetPeopleForRegistrationAsync(string buildingId, bool includeAllBuildings = false)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Persons.AsNoTracking().Where(person => !person.IsArchived);
        if (!includeAllBuildings)
        {
            var personIds = db.ApartmentOccupancies.AsNoTracking()
                .Where(item => item.BuildingId == buildingId)
                .Select(item => item.PersonId).Distinct();
            query = query.Where(person => personIds.Contains(person.PersonId));
        }
        return await query.OrderBy(person => person.DisplayName).Take(500).ToListAsync();
    }

    /// <summary> Gets or previews data through GetByBuildingAsync. </summary>
    public async Task<List<ResidentEntity>> GetByBuildingAsync(string buildingId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var rows = await (
            from occupancy in db.ApartmentOccupancies.AsNoTracking()
            join person in db.Persons.AsNoTracking() on occupancy.PersonId equals person.PersonId
            join accountRow in db.UserAccounts.AsNoTracking() on person.PersonId equals accountRow.PersonId into accountRows
            from account in accountRows.DefaultIfEmpty()
            where occupancy.BuildingId == buildingId && !person.IsArchived
                && occupancy.ValidFromUtc <= now && (occupancy.ValidToUtc == null || occupancy.ValidToUtc > now)
            orderby occupancy.ApartmentId, occupancy.IsPrimary descending, person.DisplayName
            select new { Occupancy = occupancy, Person = person, Account = account })
            .ToListAsync();
        var accountIds = rows.Where(row => row.Account != null).Select(row => row.Account!.AccountId).Distinct().ToList();
        var assignments = await GetResidentAssignmentsAsync(db, accountIds, now);
        return rows.Select(row => Map(row.Occupancy, row.Person, row.Account, assignments, now)).ToList();
    }

    /// <summary> Gets or previews data through GetByApartmentAsync. </summary>
    public async Task<List<ResidentEntity>> GetByApartmentAsync(string buildingId, string apartmentId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var rows = await (
            from occupancy in db.ApartmentOccupancies.AsNoTracking()
            join person in db.Persons.AsNoTracking() on occupancy.PersonId equals person.PersonId
            join accountRow in db.UserAccounts.AsNoTracking() on person.PersonId equals accountRow.PersonId into accountRows
            from account in accountRows.DefaultIfEmpty()
            where occupancy.BuildingId == buildingId && occupancy.ApartmentId == apartmentId && !person.IsArchived
                && occupancy.ValidFromUtc <= now && (occupancy.ValidToUtc == null || occupancy.ValidToUtc > now)
            orderby occupancy.IsPrimary descending, person.DisplayName
            select new { Occupancy = occupancy, Person = person, Account = account })
            .ToListAsync();
        var accountIds = rows.Where(row => row.Account != null).Select(row => row.Account!.AccountId).Distinct().ToList();
        var assignments = await GetResidentAssignmentsAsync(db, accountIds, now);
        return rows.Select(row => Map(row.Occupancy, row.Person, row.Account, assignments, now)).ToList();
    }

    /// <summary> Gets or previews data through GetByIdAsync. </summary>
    public async Task<ResidentEntity?> GetByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var occupancyId)) return null;
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var row = await (
            from occupancy in db.ApartmentOccupancies.AsNoTracking()
            join person in db.Persons.AsNoTracking() on occupancy.PersonId equals person.PersonId
            join accountRow in db.UserAccounts.AsNoTracking() on person.PersonId equals accountRow.PersonId into accountRows
            from account in accountRows.DefaultIfEmpty()
            where occupancy.OccupancyId == occupancyId && !person.IsArchived
            select new { Occupancy = occupancy, Person = person, Account = account })
            .SingleOrDefaultAsync();
        if (row is null) return null;
        var assignments = row.Account is null
            ? new List<UserRoleAssignmentEntity>()
            : await GetResidentAssignmentsAsync(db, new List<Guid> { row.Account.AccountId }, now);
        return Map(row.Occupancy, row.Person, row.Account, assignments, now);
    }

    /// <summary>Registers a person and an effective-dated apartment occupancy.</summary>
    /// <remarks>A normalized phone identifies a shared login when exactly one active person matches it. Apartment occupancy remains a separate, apartment-scoped record. A newly created login is pending and has no credential.</remarks>
    public async Task<Guid> CreateAsync(
        string buildingId, string apartmentId,
        string name, string phone, string whatsapp, string? email,
        bool isPrimary, bool isOwner, string createdBy,
        Guid? existingPersonId = null, bool createLoginAccount = true, Guid? registrationPersonId = null, bool allowCrossBuildingReuse = false)
    {
        if (!existingPersonId.HasValue && (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2))
            throw new ArgumentException("A valid resident name is required for a new person.");

        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(createdBy, out var parsedActor) ? parsedActor : (Guid?)null;
        var normalizedPhone = AuthHelpers.NormalizePhone(phone);
        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        if (!await db.Apartments.AnyAsync(item => item.BuildingId == buildingId && item.Id == apartmentId))
            throw new InvalidOperationException("apartment-not-found");

        PersonEntity? person = null;
        var personCreated = false;
        if (existingPersonId.HasValue)
        {
            person = await db.Persons.SingleOrDefaultAsync(item => item.PersonId == existingPersonId.Value && !item.IsArchived)
                ?? throw new InvalidOperationException("resident-person-not-found");
            if (!allowCrossBuildingReuse && !await db.ApartmentOccupancies.AnyAsync(item => item.PersonId == person.PersonId
                && item.BuildingId == buildingId))
                throw new InvalidOperationException("resident-person-outside-building-scope");
        }
        else
        {
            if (registrationPersonId.HasValue)
            {
                person = await db.Persons.SingleOrDefaultAsync(item => item.PersonId == registrationPersonId.Value);
                if (person?.IsArchived == true) throw new InvalidOperationException("resident-person-archived");
                if (person is not null && !allowCrossBuildingReuse && !await db.ApartmentOccupancies.AnyAsync(item => item.PersonId == person.PersonId
                    && item.BuildingId == buildingId))
                    throw new InvalidOperationException("resident-person-outside-building-scope");
            }
            if (person is null && !string.IsNullOrWhiteSpace(normalizedPhone))
            {
                var phoneCandidates = await db.Persons
                    .Where(item => !item.IsArchived && item.PrimaryPhone != null
                        && !db.UserAccounts.Any(account => account.PersonId == item.PersonId
                            && db.UserRoleAssignments.Any(assignment => assignment.AccountId == account.AccountId
                                && assignment.RoleKey == "superadmin" && assignment.RevokedAtUtc == null)))
                    .ToListAsync();
                var matches = phoneCandidates.Where(item => string.Equals(
                    AuthHelpers.NormalizePhone(item.PrimaryPhone), normalizedPhone, StringComparison.Ordinal))
                    .ToList();
                if (matches.Count == 1)
                {
                    person = matches[0];
                }
                else if (matches.Count > 1)
                {
                    var personIds = matches.Select(item => item.PersonId).ToHashSet();
                    var candidateAccounts = await db.UserAccounts.AsNoTracking()
                        .Where(item => personIds.Contains(item.PersonId)).ToListAsync();
                    var accountIds = candidateAccounts.Select(item => item.AccountId).ToHashSet();
                    var candidateAssignments = await db.UserRoleAssignments.AsNoTracking()
                        .Where(item => accountIds.Contains(item.AccountId)).ToListAsync();
                    var candidateCredentials = await db.UserCredentials.AsNoTracking()
                        .Where(item => accountIds.Contains(item.AccountId)).ToListAsync();
                    var candidateFactors = await db.Set<UserTwoFactorSettingEntity>().AsNoTracking()
                        .Where(item => accountIds.Contains(item.AccountId)).ToListAsync();
                    person = SelectCanonicalPerson(matches, candidateAccounts, candidateAssignments,
                        candidateCredentials, candidateFactors, DateTime.UtcNow);
                }
                if (person is not null && !allowCrossBuildingReuse
                    && !await db.ApartmentOccupancies.AnyAsync(item =>
                        item.PersonId == person.PersonId && item.BuildingId == buildingId))
                    throw new InvalidOperationException("resident-person-outside-building-scope");
            }

            if (person is null)
            {
                if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
                    throw new ArgumentException("A valid resident name is required for a new person.");
                person = new PersonEntity
                {
                    PersonId = registrationPersonId ?? Guid.NewGuid(), DisplayName = name.Trim(),
                    PrimaryPhone = string.IsNullOrWhiteSpace(normalizedPhone) ? null : normalizedPhone,
                    Whatsapp = AuthHelpers.NormalizePhone(string.IsNullOrWhiteSpace(whatsapp) ? normalizedPhone : whatsapp),
                    PrimaryEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim(), CreatedAtUtc = now
                };
                db.Persons.Add(person);
                personCreated = true;
            }
        }

        var existingOccupancy = await db.ApartmentOccupancies.SingleOrDefaultAsync(item =>
            item.PersonId == person.PersonId && item.BuildingId == buildingId && item.ApartmentId == apartmentId
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now));
        if (!personCreated && (existingPersonId.HasValue || registrationPersonId.HasValue))
        {
            var profileChanged = false;
            if (string.IsNullOrWhiteSpace(person.PrimaryPhone) && !string.IsNullOrWhiteSpace(normalizedPhone))
            {
                person.PrimaryPhone = normalizedPhone;
                profileChanged = true;
            }
            if (string.IsNullOrWhiteSpace(person.Whatsapp))
            {
                person.Whatsapp = AuthHelpers.NormalizePhone(string.IsNullOrWhiteSpace(whatsapp) ? normalizedPhone : whatsapp);
                profileChanged = true;
            }
            if (string.IsNullOrWhiteSpace(person.PrimaryEmail) && !string.IsNullOrWhiteSpace(email))
            {
                person.PrimaryEmail = email.Trim();
                profileChanged = true;
            }
            if (profileChanged) person.UpdatedAtUtc = now;
        }

        UserAccountEntity? account = null;
        var accountCreated = false;
        if (createLoginAccount)
        {
            account = await db.UserAccounts.SingleOrDefaultAsync(item => item.PersonId == person.PersonId);
            if (account is null)
            {
                account = CreateAccount(person.PersonId, now);
                db.UserAccounts.Add(account);
                accountCreated = true;
            }
            else if (account.AccountStatus == 3)
            {
                // A returning resident must re-enroll; do not silently reactivate an old credential.
                account.AccountStatus = 0;
                account.MustChangePin = true;
                account.SecurityStamp = Guid.NewGuid().ToString("N");
                var credentials = await db.UserCredentials.Where(item => item.AccountId == account.AccountId
                    && item.RevokedAtUtc == null).ToListAsync();
                foreach (var credential in credentials) credential.RevokedAtUtc = now;
            }
        }

        var residentRole = await db.RoleDefinitions.SingleOrDefaultAsync(role => role.RoleKey == "resident");
        if (residentRole is null)
        {
            residentRole = new RoleDefinitionEntity { RoleKey = "resident", DisplayName = "Resident", IsSystemRole = true, IsActive = true };
            db.RoleDefinitions.Add(residentRole);
        }
        else if (!residentRole.IsActive)
        {
            throw new InvalidOperationException("resident-role-inactive");
        }

        if (existingOccupancy is null && isPrimary)
        {
            var existingPrimary = await db.ApartmentOccupancies.Where(item =>
                item.BuildingId == buildingId && item.ApartmentId == apartmentId
                && item.IsPrimary && item.ValidToUtc == null).ToListAsync();
            foreach (var item in existingPrimary) item.IsPrimary = false;
        }

        var occupancy = existingOccupancy ?? new ApartmentOccupancyEntity
        {
            OccupancyId = Guid.NewGuid(), BuildingId = buildingId, ApartmentId = apartmentId,
            PersonId = person.PersonId, OccupancyType = isOwner ? (byte)1 : (byte)2,
            IsPrimary = isPrimary, ValidFromUtc = now, RecordedAtUtc = now, RecordedByAccountId = actorId
        };
        if (existingOccupancy is null) db.ApartmentOccupancies.Add(occupancy);

        if (account is not null)
        {
            var hasScope = await db.UserRoleAssignments.AnyAsync(item => item.AccountId == account.AccountId
                && item.RoleKey == "resident" && item.ScopeType == 2
                && item.ScopeBuildingId == buildingId && item.ScopeApartmentId == apartmentId
                && item.RevokedAtUtc == null && item.ValidFromUtc <= now
                && (item.ValidToUtc == null || item.ValidToUtc > now));
            if (!hasScope)
            {
                db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(), AccountId = account.AccountId, RoleKey = "resident",
                    ScopeType = 2, ScopeBuildingId = buildingId, ScopeApartmentId = apartmentId,
                    ValidFromUtc = now, GrantedAtUtc = now, GrantedByAccountId = actorId,
                    GrantReason = "Resident occupancy created"
                });
            }
        }

        if (personCreated)
            AddAudit(db, actorId, createdBy, buildingId, apartmentId, "resident.person.create",
                new { personId = person.PersonId }, now);
        if (accountCreated)
            AddAudit(db, actorId, createdBy, buildingId, apartmentId, "resident.account.pending.create",
                new { accountId = account!.AccountId, personId = person.PersonId, accountStatus = account.AccountStatus, credentialIssued = false }, now);
        if (existingOccupancy is null)
            AddAudit(db, actorId, createdBy, buildingId, apartmentId, "resident.occupancy.create",
                new { occupancyId = occupancy.OccupancyId, personId = person.PersonId, accountId = account?.AccountId }, now);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return occupancy.OccupancyId;
    }

    /// <summary> Updates account or operational data through UpdateAsync. </summary>
    public async Task UpdateAsync(
        string id, string name, string phone, string whatsapp, string? email,
        bool isPrimary, bool isOwner, string updatedBy)
    {
        if (!Guid.TryParse(id, out var occupancyId)) return;
        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(updatedBy, out var parsedActor) ? parsedActor : (Guid?)null;
        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var occupancy = await db.ApartmentOccupancies.SingleOrDefaultAsync(item => item.OccupancyId == occupancyId);
        if (occupancy is null) return;
        var person = await db.Persons.SingleOrDefaultAsync(item => item.PersonId == occupancy.PersonId && !item.IsArchived);
        if (person is null) return;

        var normalizedPhone = AuthHelpers.NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
            throw new ArgumentException("A valid resident name is required.");

        if (isPrimary)
        {
            var existingPrimary = await db.ApartmentOccupancies.Where(item =>
                item.BuildingId == occupancy.BuildingId && item.ApartmentId == occupancy.ApartmentId
                && item.OccupancyId != occupancyId && item.IsPrimary && item.ValidToUtc == null).ToListAsync();
            foreach (var item in existingPrimary) item.IsPrimary = false;
        }
        person.DisplayName = name.Trim();
        person.PrimaryPhone = string.IsNullOrWhiteSpace(normalizedPhone) ? null : normalizedPhone;
        person.Whatsapp = AuthHelpers.NormalizePhone(string.IsNullOrWhiteSpace(whatsapp) ? normalizedPhone : whatsapp);
        person.PrimaryEmail = email?.Trim();
        person.UpdatedAtUtc = now;
        occupancy.IsPrimary = isPrimary;
        occupancy.OccupancyType = isOwner ? (byte)1 : (byte)2;
        AddAudit(db, actorId, updatedBy, occupancy.BuildingId, occupancy.ApartmentId, "resident.update",
            new { occupancyId, personId = person.PersonId }, now);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    /// <summary>Rejects direct PIN changes from resident profile management.</summary>
    /// <exception cref="NotSupportedException">Individual PINs must be issued through verified account enrollment.</exception>
    public Task ChangePinAsync(string id, string newPin, string updatedBy)
        => throw new NotSupportedException("Use the verified individual resident account enrollment workflow.");

    /// <summary>Apartment-wide PINs are intentionally unsupported; every resident has an individual credential.</summary>
    /// <summary>Rejects apartment-wide PIN mutation because credentials are individual to each resident.</summary>
    public Task SetApartmentPinAsync(string buildingId, string apartmentId, string newPin, string? updatedBy)
        => throw new NotSupportedException("Shared apartment PINs are disabled. Issue an individual resident credential after identity verification.");

    /// <summary>Rejects the legacy apartment PIN alias.</summary>
    public Task UpdateResidentPinAsync(string buildingId, string apartmentId, string newPin)
        => SetApartmentPinAsync(buildingId, apartmentId, newPin, "system");

    /// <summary> Updates account or operational data through SetDisabledAsync. </summary>
    public async Task SetDisabledAsync(string id, bool disabled, string? reason, string updatedBy)
    {
        if (!Guid.TryParse(id, out var occupancyId)) return;
        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(updatedBy, out var parsedActor) ? parsedActor : (Guid?)null;
        await using var db = await _factory.CreateDbContextAsync();
        var occupancy = await db.ApartmentOccupancies.SingleOrDefaultAsync(item => item.OccupancyId == occupancyId);
        if (occupancy is null) return;
        var accountId = await db.UserAccounts.Where(item => item.PersonId == occupancy.PersonId)
            .Select(item => item.AccountId).SingleOrDefaultAsync();
        if (accountId == Guid.Empty) return;
        var assignment = await db.UserRoleAssignments.Where(item => item.AccountId == accountId
            && item.RoleKey == "resident" && item.ScopeType == 2
            && item.ScopeBuildingId == occupancy.BuildingId && item.ScopeApartmentId == occupancy.ApartmentId
            && item.RevokedAtUtc == null).ToListAsync();
        if (disabled)
        {
            foreach (var item in assignment)
            {
                item.RevokedAtUtc = now;
                item.RevokedByAccountId = actorId ?? accountId;
                item.RevocationReason = string.IsNullOrWhiteSpace(reason) ? "Resident access disabled" : reason.Trim();
            }
        }
        else if (assignment.Count == 0)
        {
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = occupancy.BuildingId, ScopeApartmentId = occupancy.ApartmentId,
                ValidFromUtc = now, GrantedAtUtc = now, GrantedByAccountId = actorId,
                GrantReason = "Resident access re-enabled"
            });
        }
        AddAudit(db, actorId, updatedBy, occupancy.BuildingId, occupancy.ApartmentId,
            disabled ? "resident.disable" : "resident.enable", new { occupancyId, reason }, now);
        await db.SaveChangesAsync();
    }

    /// <summary>Moves an existing person to another apartment in the same building without replacing the person or account.</summary>
    public async Task<Guid> MoveAsync(string occupancyId, string targetApartmentId, string movedBy)
    {
        if (!Guid.TryParse(occupancyId, out var parsedOccupancyId))
            throw new ArgumentException("A valid occupancy identifier is required.");
        if (string.IsNullOrWhiteSpace(targetApartmentId))
            throw new ArgumentException("A target apartment is required.");

        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(movedBy, out var parsedActor) ? parsedActor : (Guid?)null;
        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var current = await db.ApartmentOccupancies.SingleOrDefaultAsync(item => item.OccupancyId == parsedOccupancyId)
            ?? throw new InvalidOperationException("resident-occupancy-not-found");
        if (current.ValidToUtc.HasValue && current.ValidToUtc <= now)
            throw new InvalidOperationException("resident-occupancy-not-active");
        if (!await db.Apartments.AnyAsync(item => item.BuildingId == current.BuildingId && item.Id == targetApartmentId))
            throw new InvalidOperationException("target-apartment-not-found");
        if (string.Equals(current.ApartmentId, targetApartmentId, StringComparison.Ordinal))
            return current.OccupancyId;

        var targetOccupancy = await db.ApartmentOccupancies.SingleOrDefaultAsync(item =>
            item.PersonId == current.PersonId && item.BuildingId == current.BuildingId
            && item.ApartmentId == targetApartmentId && item.ValidFromUtc <= now
            && (item.ValidToUtc == null || item.ValidToUtc > now));
        if (targetOccupancy is null)
        {
            if (current.IsPrimary)
            {
                var otherPrimary = await db.ApartmentOccupancies.Where(item => item.BuildingId == current.BuildingId
                    && item.ApartmentId == targetApartmentId && item.IsPrimary && item.ValidToUtc == null).ToListAsync();
                foreach (var item in otherPrimary) item.IsPrimary = false;
            }
            targetOccupancy = new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), BuildingId = current.BuildingId, ApartmentId = targetApartmentId,
                PersonId = current.PersonId, OccupancyType = current.OccupancyType, IsPrimary = current.IsPrimary,
                ValidFromUtc = now, RecordedAtUtc = now, RecordedByAccountId = actorId
            };
            db.ApartmentOccupancies.Add(targetOccupancy);
        }

        current.ValidToUtc = now;
        var account = await db.UserAccounts.SingleOrDefaultAsync(item => item.PersonId == current.PersonId);
        if (account is not null)
        {
            var oldScopes = await db.UserRoleAssignments.Where(item => item.AccountId == account.AccountId
                && item.RoleKey == "resident" && item.ScopeBuildingId == current.BuildingId
                && item.ScopeApartmentId == current.ApartmentId && item.RevokedAtUtc == null).ToListAsync();
            foreach (var assignment in oldScopes)
            {
                assignment.RevokedAtUtc = now;
                assignment.RevokedByAccountId = actorId ?? account.AccountId;
                assignment.RevocationReason = "Resident occupancy moved";
            }

            var targetScopeExists = await db.UserRoleAssignments.AnyAsync(item => item.AccountId == account.AccountId
                && item.RoleKey == "resident" && item.ScopeType == 2
                && item.ScopeBuildingId == current.BuildingId && item.ScopeApartmentId == targetApartmentId
                && item.RevokedAtUtc == null && item.ValidFromUtc <= now
                && (item.ValidToUtc == null || item.ValidToUtc > now));
            if (!targetScopeExists)
            {
                db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
                {
                    RoleAssignmentId = Guid.NewGuid(), AccountId = account.AccountId, RoleKey = "resident",
                    ScopeType = 2, ScopeBuildingId = current.BuildingId, ScopeApartmentId = targetApartmentId,
                    ValidFromUtc = now, GrantedAtUtc = now, GrantedByAccountId = actorId,
                    GrantReason = "Resident occupancy moved"
                });
            }
        }

        AddAudit(db, actorId, movedBy, current.BuildingId, targetApartmentId, "resident.occupancy.move",
            new { personId = current.PersonId, accountId = account?.AccountId,
                fromOccupancyId = current.OccupancyId, toOccupancyId = targetOccupancy.OccupancyId,
                fromApartmentId = current.ApartmentId, toApartmentId = targetApartmentId }, now);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return targetOccupancy.OccupancyId;
    }

    /// <summary> Removes or revokes data through DeleteAsync. </summary>
    public async Task DeleteAsync(string id, string deletedBy)
    {
        if (!Guid.TryParse(id, out var occupancyId)) return;
        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(deletedBy, out var parsedActor) ? parsedActor : (Guid?)null;
        await using var db = await _factory.CreateDbContextAsync();
        var occupancy = await db.ApartmentOccupancies.SingleOrDefaultAsync(item => item.OccupancyId == occupancyId);
        if (occupancy is null) return;
        occupancy.ValidToUtc = now;
        var accountId = await db.UserAccounts.Where(item => item.PersonId == occupancy.PersonId)
            .Select(item => item.AccountId).SingleOrDefaultAsync();
        if (accountId != Guid.Empty)
        {
            var assignments = await db.UserRoleAssignments.Where(item => item.AccountId == accountId
                && item.RoleKey == "resident" && item.ScopeBuildingId == occupancy.BuildingId
                && item.ScopeApartmentId == occupancy.ApartmentId && item.RevokedAtUtc == null).ToListAsync();
            foreach (var assignment in assignments)
            {
                assignment.RevokedAtUtc = now;
                assignment.RevokedByAccountId = actorId ?? accountId;
                assignment.RevocationReason = "Resident occupancy removed";
            }
            var revokedAssignmentIds = assignments.Select(item => item.RoleAssignmentId).ToArray();
            var hasOtherRole = await db.UserRoleAssignments.AnyAsync(item => item.AccountId == accountId
                && !revokedAssignmentIds.Contains(item.RoleAssignmentId)
                && item.RevokedAtUtc == null && item.ValidFromUtc <= now
                && (item.ValidToUtc == null || item.ValidToUtc > now));
            if (!hasOtherRole)
            {
                var account = await db.UserAccounts.SingleAsync(item => item.AccountId == accountId);
                account.AccountStatus = 3;
                account.SecurityStamp = Guid.NewGuid().ToString("N");
            }
        }
        AddAudit(db, actorId, deletedBy, occupancy.BuildingId, occupancy.ApartmentId,
            "resident.occupancy.remove", new { occupancyId }, now);
        await db.SaveChangesAsync();
    }

    /// <summary> Gets or previews data through CountAsync. </summary>
    public async Task<int> CountAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        return await db.ApartmentOccupancies.CountAsync(item =>
            item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now));
    }

    /// <summary>
    /// Rebuilds canonical apartment occupancy and resident-role links from the legacy apartment owner/contact fields.
    /// Shared normalized phones resolve to one Person and one UserAccount, while every apartment keeps its own occupancy scope.
    /// </summary>
    public async Task<MigrationResult> MigrateFromApartmentsAsync(
        string? buildingId, bool dryRun, string? actorId = null)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var result = new MigrationResult();
        var now = DateTime.UtcNow;
        var parsedActorId = Guid.TryParse(actorId, out var parsed) ? parsed : (Guid?)null;

        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = dryRun
            ? null
            : await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (!string.IsNullOrWhiteSpace(buildingId)
            && !await db.Buildings.AnyAsync(item => item.Id == buildingId))
            throw new InvalidOperationException("migration-building-not-found");

        var apartments = await db.Apartments.AsNoTracking()
            .Where(item => string.IsNullOrWhiteSpace(buildingId) || item.BuildingId == buildingId)
            .OrderBy(item => item.BuildingId).ThenBy(item => item.Number).ThenBy(item => item.Id)
            .ToListAsync();
        result.BuildingsScanned = apartments.Select(item => item.BuildingId)
            .Distinct(StringComparer.Ordinal).Count();

        var sourceRows = apartments
            .Where(item => !string.IsNullOrWhiteSpace(item.Owner))
            .ToList();
        result.EntriesFound = sourceRows.Count;
        var relevantPhones = sourceRows.Select(item => AuthHelpers.NormalizePhone(item.Phone))
            .Where(item => !string.IsNullOrWhiteSpace(item)).ToHashSet(StringComparer.Ordinal);
        var mergedPersonRows = await MergeDuplicatePhoneIdentityGroupsAsync(db, now, relevantPhones, dryRun);
        if (!dryRun && mergedPersonRows > 0)
            db.ChangeTracker.Clear();

        var persons = await db.Persons.Where(item => !item.IsArchived).ToListAsync();
        var accounts = await db.UserAccounts.ToListAsync();
        var credentials = await db.UserCredentials.ToListAsync();
        var occupancies = await db.ApartmentOccupancies.ToListAsync();
        var assignments = await db.UserRoleAssignments.ToListAsync();
        var twoFactorSettings = await db.Set<UserTwoFactorSettingEntity>().AsNoTracking().ToListAsync();
        if (dryRun && mergedPersonRows > 0)
            SimulateDuplicatePhoneMerges(persons, accounts, credentials, assignments, occupancies,
                twoFactorSettings, relevantPhones, now);

        var accountsWithPreexistingCredentials = credentials
            .Where(item => item.CredentialType == 0 && item.RevokedAtUtc == null
                && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now))
            .Select(item => item.AccountId).ToHashSet();
        var superAdminAccountIds = assignments.Where(item => item.RoleKey == "superadmin"
                && item.RevokedAtUtc == null)
            .Select(item => item.AccountId).ToHashSet();
        var superAdminPersonIds = accounts.Where(item => superAdminAccountIds.Contains(item.AccountId))
            .Select(item => item.PersonId).ToHashSet();

        var residentRole = await db.RoleDefinitions.SingleOrDefaultAsync(item => item.RoleKey == "resident");
        if (residentRole is null)
        {
            residentRole = new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsSystemRole = true, IsActive = true
            };
            db.RoleDefinitions.Add(residentRole);
        }
        else if (!residentRole.IsActive)
        {
            throw new InvalidOperationException("resident-role-inactive");
        }

        var changed = false;
        foreach (var apartment in sourceRows)
        {
            var entryChanged = false;
            try
            {
                var normalizedPhone = AuthHelpers.NormalizePhone(apartment.Phone);
                var normalizedName = NormalizePersonName(apartment.Owner);
                var activeOccupancies = occupancies.Where(item =>
                    item.BuildingId == apartment.BuildingId && item.ApartmentId == apartment.Id
                    && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now))
                    .ToList();

                if (activeOccupancies.Count > 1)
                {
                    result.EntriesFailed++;
                    result.Errors.Add($"Apartment {apartment.BuildingId}/{apartment.Id}: multiple active occupancy records require review.");
                    continue;
                }

                PersonEntity? person = null;
                if (activeOccupancies.Count == 1)
                {
                    person = persons.SingleOrDefault(item => item.PersonId == activeOccupancies[0].PersonId);
                    if (person is null)
                    {
                        result.EntriesFailed++;
                        result.Errors.Add($"Apartment {apartment.BuildingId}/{apartment.Id}: linked person record is unavailable.");
                        continue;
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(normalizedPhone))
                    {
                        var phoneMatches = persons.Where(item => !superAdminPersonIds.Contains(item.PersonId)
                            && string.Equals(AuthHelpers.NormalizePhone(item.PrimaryPhone), normalizedPhone, StringComparison.Ordinal))
                            .ToList();
                        if (phoneMatches.Count == 1)
                            person = phoneMatches[0];
                        else if (phoneMatches.Count > 1)
                            person = SelectCanonicalPerson(phoneMatches, accounts, assignments, credentials, twoFactorSettings, now);
                    }
                    else
                    {
                        var nameMatches = persons.Where(item => !superAdminPersonIds.Contains(item.PersonId)
                            && NormalizePersonName(item.DisplayName) == normalizedName).ToList();
                        if (nameMatches.Count == 1) person = nameMatches[0];
                        else if (nameMatches.Count > 1)
                        {
                            result.EntriesFailed++;
                            result.Errors.Add($"Apartment {apartment.BuildingId}/{apartment.Id}: owner name is not unique and phone is missing.");
                            continue;
                        }
                    }

                    if (person is null)
                    {
                        person = new PersonEntity
                        {
                            PersonId = Guid.NewGuid(),
                            DisplayName = apartment.Owner.Trim(),
                            PrimaryPhone = string.IsNullOrWhiteSpace(normalizedPhone) ? null : normalizedPhone,
                            Whatsapp = string.IsNullOrWhiteSpace(normalizedPhone) ? null : normalizedPhone,
                            PrimaryEmail = string.IsNullOrWhiteSpace(apartment.Email) ? null : apartment.Email.Trim(),
                            CreatedAtUtc = now
                        };
                        persons.Add(person);
                        db.Persons.Add(person);
                        entryChanged = true;
                    }
                }

                if (string.IsNullOrWhiteSpace(person.DisplayName) && !string.IsNullOrWhiteSpace(apartment.Owner))
                {
                    person.DisplayName = apartment.Owner.Trim();
                    entryChanged = true;
                }
                if (string.IsNullOrWhiteSpace(person.PrimaryPhone) && !string.IsNullOrWhiteSpace(normalizedPhone))
                {
                    person.PrimaryPhone = normalizedPhone;
                    entryChanged = true;
                }
                if (string.IsNullOrWhiteSpace(person.Whatsapp) && !string.IsNullOrWhiteSpace(normalizedPhone))
                {
                    person.Whatsapp = normalizedPhone;
                    entryChanged = true;
                }
                if (string.IsNullOrWhiteSpace(person.PrimaryEmail) && !string.IsNullOrWhiteSpace(apartment.Email))
                {
                    person.PrimaryEmail = apartment.Email.Trim();
                    entryChanged = true;
                }

                var account = accounts.SingleOrDefault(item => item.PersonId == person.PersonId);
                if (account is null)
                {
                    account = CreateAccount(person.PersonId, now);
                    accounts.Add(account);
                    db.UserAccounts.Add(account);
                    entryChanged = true;
                }
                if (string.IsNullOrWhiteSpace(account.NormalizedEmail) && !string.IsNullOrWhiteSpace(person.PrimaryEmail))
                {
                    account.NormalizedEmail = person.PrimaryEmail.Trim().ToUpperInvariant();
                    entryChanged = true;
                }
                if (account.AccountStatus == 3)
                {
                    var hasOtherActiveRole = assignments.Any(item => item.AccountId == account.AccountId
                        && item.RoleKey != "resident" && item.RevokedAtUtc == null
                        && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now));
                    if (!hasOtherActiveRole)
                    {
                        account.AccountStatus = 0;
                        account.MustChangePin = true;
                        account.SecurityStamp = Guid.NewGuid().ToString("N");
                        foreach (var credential in credentials.Where(item =>
                            item.AccountId == account.AccountId && item.RevokedAtUtc == null))
                            credential.RevokedAtUtc = now;
                        accountsWithPreexistingCredentials.Remove(account.AccountId);
                        entryChanged = true;
                    }
                }

                // Preserve usable legacy apartment PINs by hashing them onto the shared account.
                // Once an account already has a credential, never replace or weaken that credential.
                var legacyPin = apartment.Pin?.Trim();
                if (!accountsWithPreexistingCredentials.Contains(account.AccountId) && IsValidPin(legacyPin))
                {
                    var accountCredentials = credentials.Where(item => item.AccountId == account.AccountId
                        && item.CredentialType == 0 && item.RevokedAtUtc == null
                        && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now)).ToList();
                    var pinAlreadyImported = accountCredentials.Any(item =>
                        (item.HashAlgorithm == "BCrypt.Net-Next" || item.HashAlgorithm == "bcrypt-v1")
                        && _hasher.VerifyPin(legacyPin!, item.CredentialHash));
                    if (!pinAlreadyImported)
                    {
                        var credential = new UserCredentialEntity
                        {
                            CredentialId = Guid.NewGuid(),
                            AccountId = account.AccountId,
                            CredentialType = 0,
                            CredentialHash = _hasher.HashPin(legacyPin!),
                            HashAlgorithm = "BCrypt.Net-Next",
                            CreatedAtUtc = now
                        };
                        credentials.Add(credential);
                        db.UserCredentials.Add(credential);
                        account.MustChangePin = true;
                        if (account.AccountStatus == 0 && accountCredentials.Count == 0)
                        {
                            account.AccountStatus = 1;
                            account.SecurityStamp = Guid.NewGuid().ToString("N");
                        }
                        entryChanged = true;
                    }
                }

                if (activeOccupancies.Count == 0)
                {
                    var hasPrimary = occupancies.Any(item => item.BuildingId == apartment.BuildingId
                        && item.ApartmentId == apartment.Id && item.IsPrimary
                        && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now));
                    var occupancy = new ApartmentOccupancyEntity
                    {
                        OccupancyId = Guid.NewGuid(),
                        BuildingId = apartment.BuildingId,
                        ApartmentId = apartment.Id,
                        PersonId = person.PersonId,
                        OccupancyType = 1,
                        IsPrimary = !hasPrimary,
                        ValidFromUtc = now,
                        RecordedAtUtc = now,
                        RecordedByAccountId = parsedActorId,
                        Notes = "Migrated from Apartments.Owner, Phone and Email; apartment data remains independent."
                    };
                    occupancies.Add(occupancy);
                    db.ApartmentOccupancies.Add(occupancy);
                    entryChanged = true;
                }
                else if (activeOccupancies[0].PersonId != person.PersonId)
                {
                    result.EntriesFailed++;
                    result.Errors.Add($"Apartment {apartment.BuildingId}/{apartment.Id}: existing occupancy belongs to a different person.");
                    continue;
                }

                var residentScopeExists = assignments.Any(item =>
                    item.AccountId == account.AccountId && item.RoleKey == "resident" && item.ScopeType == 2
                    && item.ScopeBuildingId == apartment.BuildingId && item.ScopeApartmentId == apartment.Id
                    && item.RevokedAtUtc == null && item.ValidFromUtc <= now
                    && (item.ValidToUtc == null || item.ValidToUtc > now));
                if (!residentScopeExists)
                {
                    var assignment = new UserRoleAssignmentEntity
                    {
                        RoleAssignmentId = Guid.NewGuid(),
                        AccountId = account.AccountId,
                        RoleKey = "resident",
                        ScopeType = 2,
                        ScopeBuildingId = apartment.BuildingId,
                        ScopeApartmentId = apartment.Id,
                        ValidFromUtc = now,
                        GrantedAtUtc = now,
                        GrantedByAccountId = parsedActorId,
                        GrantReason = "Resident scope migrated from the apartment owner record."
                    };
                    assignments.Add(assignment);
                    db.UserRoleAssignments.Add(assignment);
                    entryChanged = true;
                }

                if (entryChanged)
                {
                    changed = true;
                    result.EntriesMigrated++;
                }
                else
                {
                    result.EntriesSkipped++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.EntriesFailed++;
                result.Errors.Add($"Apartment {apartment.BuildingId}/{apartment.Id}: migration failed ({ex.GetType().Name}).");
                _logger.LogWarning(ex, "Apartment resident migration failed for {BuildingId}/{ApartmentId}.",
                    apartment.BuildingId, apartment.Id);
            }
        }

        if (!dryRun)
        {
            await db.SaveChangesAsync();
            await transaction!.CommitAsync();
            if (changed || mergedPersonRows > 0)
            {
                await _audit.LogAsync(
                    action: "resident.apartment_occupancy_migration",
                    buildingId: buildingId,
                    userId: actorId ?? "system",
                    userRole: "superadmin",
                    metadata: new
                    {
                        buildingId,
                        entriesFound = result.EntriesFound,
                        entriesMigrated = result.EntriesMigrated,
                        entriesSkipped = result.EntriesSkipped,
                        entriesFailed = result.EntriesFailed,
                        mergedPersonRows,
                        dryRun = false
                    },
                    severity: result.EntriesFailed > 0 ? "warning" : "info");
            }
        }

        timer.Stop();
        result.Duration = timer.Elapsed;
        result.Details.Add($"Apartments scanned: {sourceRows.Count}");
        result.Details.Add($"Duplicate phone identity rows consolidated: {mergedPersonRows}");
        result.Details.Add($"Occupancy links planned or created: {result.EntriesMigrated}");
        result.Details.Add($"Already complete: {result.EntriesSkipped}");
        result.Details.Add($"Needs review: {result.EntriesFailed}");
        return result;
    }

    private async Task<int> MergeDuplicatePhoneIdentityGroupsAsync(
        AppDbContext db, DateTime now, HashSet<string> relevantPhones, bool dryRun)
    {
        if (relevantPhones.Count == 0) return 0;

        var persons = await db.Persons.Where(item => !item.IsArchived).ToListAsync();
        var accounts = await db.UserAccounts.ToListAsync();
        var assignments = await db.UserRoleAssignments.AsNoTracking().ToListAsync();
        var credentials = await db.UserCredentials.AsNoTracking().ToListAsync();
        var factors = await db.Set<UserTwoFactorSettingEntity>().AsNoTracking().ToListAsync();
        var superAdminIds = assignments.Where(item => item.RoleKey == "superadmin" && item.RevokedAtUtc == null)
            .Select(item => item.AccountId).ToHashSet();
        var superAdminPersonIds = accounts.Where(item => superAdminIds.Contains(item.AccountId))
            .Select(item => item.PersonId).ToHashSet();

        var groups = persons.Where(item => !superAdminPersonIds.Contains(item.PersonId))
            .Select(item => new { Person = item, Phone = AuthHelpers.NormalizePhone(item.PrimaryPhone) })
            .Where(item => !string.IsNullOrWhiteSpace(item.Phone) && relevantPhones.Contains(item.Phone))
            .GroupBy(item => item.Phone!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToList();

        var mergedPersonRows = 0;
        foreach (var group in groups)
        {
            var candidates = group.Select(item => item.Person).ToList();
            var canonicalPerson = SelectCanonicalPerson(candidates, accounts, assignments, credentials, factors, now);
            var duplicatePersons = candidates.Where(item => item.PersonId != canonicalPerson.PersonId).ToList();
            var candidatePersonIds = candidates.Select(item => item.PersonId).ToHashSet();
            var candidateAccounts = accounts.Where(item => candidatePersonIds.Contains(item.PersonId)).ToList();
            var canonicalAccount = accounts.SingleOrDefault(item => item.PersonId == canonicalPerson.PersonId);

            if (candidateAccounts.Count > 1)
            {
                var externalPairs = candidateAccounts
                    .Where(item => !string.IsNullOrWhiteSpace(item.ExternalProvider)
                        && !string.IsNullOrWhiteSpace(item.ExternalSubject))
                    .Select(item => item.ExternalProvider + "|" + item.ExternalSubject)
                    .Distinct(StringComparer.Ordinal).ToList();
                if (externalPairs.Count > 1)
                    throw new InvalidOperationException("phone-identity-merge-has-multiple-external-logins");

                var accountIds = candidateAccounts.Select(item => item.AccountId).ToHashSet();
                var legacyMaps = await db.Set<LegacyIdentityMapEntity>()
                    .Where(item => item.AccountId != null && accountIds.Contains(item.AccountId.Value))
                    .ToListAsync();

                var accountFactors = await db.Set<UserTwoFactorSettingEntity>()
                    .Where(item => accountIds.Contains(item.AccountId)).ToListAsync();
                if (accountFactors.Count(item => item.IsEnabled) > 1)
                    throw new InvalidOperationException("phone-identity-merge-has-multiple-enabled-two-factor-settings");
                if (dryRun)
                {
                    mergedPersonRows += duplicatePersons.Count;
                    continue;
                }

                var preferences = await db.Set<UserPreferenceEntity>()
                    .Where(item => accountIds.Contains(item.AccountId)).ToListAsync();
                foreach (var preferenceGroup in preferences.GroupBy(item => item.PreferenceKey))
                {
                    var keep = preferenceGroup.FirstOrDefault(item => item.AccountId == canonicalAccount!.AccountId)
                        ?? preferenceGroup.OrderByDescending(item => item.UpdatedAtUtc).First();
                    foreach (var duplicate in preferenceGroup.Where(item => item.PreferenceId != keep.PreferenceId))
                        db.Remove(duplicate);
                    keep.AccountId = canonicalAccount!.AccountId;
                }

                var enabledFactor = accountFactors.SingleOrDefault(item => item.IsEnabled);
                var keepFactor = enabledFactor
                    ?? accountFactors.FirstOrDefault(item => item.AccountId == canonicalAccount!.AccountId)
                    ?? accountFactors.OrderByDescending(item => item.UpdatedAtUtc).FirstOrDefault();
                foreach (var duplicate in accountFactors.Where(item =>
                    keepFactor == null || item.AccountId != keepFactor.AccountId))
                    db.Remove(duplicate);
                if (keepFactor != null)
                    keepFactor.AccountId = canonicalAccount!.AccountId;

                if (legacyMaps.Count > 1)
                {
                    var keepMap = legacyMaps.FirstOrDefault(item => item.AccountId == canonicalAccount!.AccountId)
                        ?? legacyMaps.OrderByDescending(item => item.MappingStatus == (byte)LegacyIdentityMappingStatus.Linked
                            || item.MappingStatus == (byte)LegacyIdentityMappingStatus.Separate).First();
                    foreach (var duplicateMap in legacyMaps.Where(item =>
                        item.LegacyIdentityMapId != keepMap.LegacyIdentityMapId))
                    {
                        duplicateMap.MappingStatus = (byte)LegacyIdentityMappingStatus.ManualReview;
                        duplicateMap.PersonId = null;
                        duplicateMap.AccountId = null;
                        duplicateMap.ReviewedAtUtc = null;
                        duplicateMap.ReviewedByAccountId = null;
                        duplicateMap.ReviewReason = "Source identity retained for manual review after shared-phone account consolidation.";
                    }
                }

                var preferredAccount = candidateAccounts
                    .OrderByDescending(item => item.AccountStatus == 1)
                    .ThenByDescending(item => item.LastLoginAtUtc)
                    .First();
                canonicalAccount!.AccountStatus = candidateAccounts.Any(item => item.AccountStatus == 1) ? (byte)1
                    : candidateAccounts.Any(item => item.AccountStatus == 0) ? (byte)0
                    : candidateAccounts.Any(item => item.AccountStatus == 2) ? (byte)2 : (byte)3;
                canonicalAccount.MustChangePin = candidateAccounts.Any(item => item.MustChangePin);
                canonicalAccount.NormalizedEmail ??= candidateAccounts
                    .Select(item => item.NormalizedEmail).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
                if (canonicalAccount.ExternalProvider is null && externalPairs.Count == 1)
                {
                    canonicalAccount.ExternalProvider = preferredAccount.ExternalProvider;
                    canonicalAccount.ExternalSubject = preferredAccount.ExternalSubject;
                }
                canonicalAccount.LastLoginAtUtc = candidateAccounts
                    .Where(item => item.LastLoginAtUtc != null).Select(item => item.LastLoginAtUtc)
                    .OrderByDescending(item => item).FirstOrDefault();
                canonicalAccount.CreatedAtUtc = candidateAccounts.Min(item => item.CreatedAtUtc);
                canonicalAccount.SecurityStamp = Guid.NewGuid().ToString("N");

                if (!dryRun)
                {
                    await db.SaveChangesAsync();
                    foreach (var duplicateAccount in candidateAccounts.Where(item =>
                        item.AccountId != canonicalAccount.AccountId))
                    {
                        await RepointSqlReferencesAsync(db, "dbo.UserAccounts", "AccountId",
                            duplicateAccount.AccountId, canonicalAccount.AccountId);
                        await db.Database.ExecuteSqlRawAsync(
                            "UPDATE [dbo].[AuditEvents] SET [ActorAccountId] = {0} WHERE [ActorAccountId] = {1}",
                            canonicalAccount.AccountId, duplicateAccount.AccountId);
                    }
                    db.UserAccounts.RemoveRange(candidateAccounts.Where(item =>
                        item.AccountId != canonicalAccount.AccountId));
                    await db.SaveChangesAsync();
                }
            }
            if (dryRun)
            {
                mergedPersonRows += duplicatePersons.Count;
                continue;
            }

            foreach (var duplicatePerson in duplicatePersons)
            {
                if (string.IsNullOrWhiteSpace(canonicalPerson.PrimaryEmail))
                    canonicalPerson.PrimaryEmail = duplicatePerson.PrimaryEmail;
                if (string.IsNullOrWhiteSpace(canonicalPerson.Whatsapp))
                    canonicalPerson.Whatsapp = duplicatePerson.Whatsapp;
                if (string.IsNullOrWhiteSpace(canonicalPerson.PhotoUrl))
                    canonicalPerson.PhotoUrl = duplicatePerson.PhotoUrl;
            }
            canonicalPerson.PrimaryPhone = AuthHelpers.NormalizePhone(canonicalPerson.PrimaryPhone)
                ?? group.Key;
            canonicalPerson.UpdatedAtUtc = now;

            if (!dryRun)
            {
                foreach (var duplicatePerson in duplicatePersons)
                    await RepointSqlReferencesAsync(db, "dbo.Persons", "PersonId",
                        duplicatePerson.PersonId, canonicalPerson.PersonId);
                db.Persons.RemoveRange(duplicatePersons);
                await db.SaveChangesAsync();
            }

            mergedPersonRows += duplicatePersons.Count;
        }

        return mergedPersonRows;
    }

    private static PersonEntity SelectCanonicalPerson(
        IReadOnlyCollection<PersonEntity> candidates,
        IReadOnlyCollection<UserAccountEntity> accounts,
        IReadOnlyCollection<UserRoleAssignmentEntity> assignments,
        IReadOnlyCollection<UserCredentialEntity> credentials,
        IReadOnlyCollection<UserTwoFactorSettingEntity> factors,
        DateTime now)
    {
        var activeAssignments = assignments.Where(item => item.RevokedAtUtc == null
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)).ToList();
        return candidates
            .OrderByDescending(person => factors.Any(item =>
                accounts.Any(account => account.PersonId == person.PersonId && account.AccountId == item.AccountId)
                && item.IsEnabled))
            .ThenByDescending(person => accounts.Any(account => account.PersonId == person.PersonId))
            .ThenByDescending(person => activeAssignments.Count(item =>
                accounts.Any(account => account.PersonId == person.PersonId && account.AccountId == item.AccountId)
                && item.RoleKey == "resident" && item.ScopeType == 2))
            .ThenByDescending(person => activeAssignments.Count(item =>
                accounts.Any(account => account.PersonId == person.PersonId && account.AccountId == item.AccountId)
                && (item.RoleKey == "admin" || item.RoleKey == "building-admin")))
            .ThenByDescending(person => accounts.Any(account =>
                account.PersonId == person.PersonId && account.AccountStatus == 1))
            .ThenByDescending(person => credentials.Count(item =>
                accounts.Any(account => account.PersonId == person.PersonId && account.AccountId == item.AccountId)
                && item.CredentialType == 0 && item.RevokedAtUtc == null
                && (item.ExpiresAtUtc == null || item.ExpiresAtUtc > now)))
            .ThenByDescending(person => accounts.Where(account => account.PersonId == person.PersonId)
                .Select(account => account.LastLoginAtUtc).Max())
            .ThenBy(person => person.CreatedAtUtc)
            .ThenBy(person => person.PersonId)
            .First();
    }

    private static void SimulateDuplicatePhoneMerges(
        List<PersonEntity> persons,
        List<UserAccountEntity> accounts,
        List<UserCredentialEntity> credentials,
        List<UserRoleAssignmentEntity> assignments,
        List<ApartmentOccupancyEntity> occupancies,
        IReadOnlyCollection<UserTwoFactorSettingEntity> factors,
        HashSet<string> relevantPhones,
        DateTime now)
    {
        var superAdminIds = assignments.Where(item => item.RoleKey == "superadmin" && item.RevokedAtUtc == null)
            .Select(item => item.AccountId).ToHashSet();
        var superAdminPersonIds = accounts.Where(item => superAdminIds.Contains(item.AccountId))
            .Select(item => item.PersonId).ToHashSet();
        var groups = persons.Where(item => !superAdminPersonIds.Contains(item.PersonId))
            .Select(item => new { Person = item, Phone = AuthHelpers.NormalizePhone(item.PrimaryPhone) })
            .Where(item => !string.IsNullOrWhiteSpace(item.Phone) && relevantPhones.Contains(item.Phone))
            .GroupBy(item => item.Phone!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).ToList();

        foreach (var group in groups)
        {
            var candidates = group.Select(item => item.Person).ToList();
            var canonical = SelectCanonicalPerson(candidates, accounts, assignments, credentials, factors, now);
            var personIds = candidates.Select(item => item.PersonId).ToHashSet();
            var candidateAccounts = accounts.Where(item => personIds.Contains(item.PersonId)).ToList();
            var canonicalAccount = accounts.SingleOrDefault(item => item.PersonId == canonical.PersonId);
            if (canonicalAccount != null && candidateAccounts.Count > 1)
            {
                canonicalAccount.AccountStatus = candidateAccounts.Any(item => item.AccountStatus == 1) ? (byte)1
                    : candidateAccounts.Any(item => item.AccountStatus == 0) ? (byte)0
                    : candidateAccounts.Any(item => item.AccountStatus == 2) ? (byte)2 : (byte)3;
                canonicalAccount.MustChangePin = candidateAccounts.Any(item => item.MustChangePin);
                var sourceAccountIds = candidateAccounts.Select(item => item.AccountId).ToHashSet();
                foreach (var credential in credentials.Where(item => sourceAccountIds.Contains(item.AccountId)))
                    credential.AccountId = canonicalAccount.AccountId;
                foreach (var assignment in assignments.Where(item => sourceAccountIds.Contains(item.AccountId)))
                {
                    assignment.AccountId = canonicalAccount.AccountId;
                    if (assignment.GrantedByAccountId.HasValue && sourceAccountIds.Contains(assignment.GrantedByAccountId.Value))
                        assignment.GrantedByAccountId = canonicalAccount.AccountId;
                    if (assignment.RevokedByAccountId.HasValue && sourceAccountIds.Contains(assignment.RevokedByAccountId.Value))
                        assignment.RevokedByAccountId = canonicalAccount.AccountId;
                }
                accounts.RemoveAll(item => item.AccountId != canonicalAccount.AccountId
                    && sourceAccountIds.Contains(item.AccountId));
            }

            var duplicatePersonIds = personIds.Where(item => item != canonical.PersonId).ToHashSet();
            foreach (var occupancy in occupancies.Where(item => duplicatePersonIds.Contains(item.PersonId)))
                occupancy.PersonId = canonical.PersonId;
            persons.RemoveAll(item => duplicatePersonIds.Contains(item.PersonId));
        }
    }

    private static Task<int> RepointSqlReferencesAsync(
        AppDbContext db, string principalTable, string principalKeyColumn, Guid sourceId, Guid targetId)
    {
        var sql = $@"
DECLARE @target uniqueidentifier = {{0}};
DECLARE @source uniqueidentifier = {{1}};
DECLARE @statement nvarchar(max) = N'';
SELECT @statement = @statement
    + N'UPDATE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fkc.parent_object_id))
    + N'.' + QUOTENAME(OBJECT_NAME(fkc.parent_object_id))
    + N' SET ' + QUOTENAME(dependentColumn.name) + N' = @target WHERE '
    + QUOTENAME(dependentColumn.name) + N' = @source;'
FROM sys.foreign_key_columns fkc
INNER JOIN sys.columns dependentColumn
    ON dependentColumn.object_id = fkc.parent_object_id
    AND dependentColumn.column_id = fkc.parent_column_id
INNER JOIN sys.columns principalColumn
    ON principalColumn.object_id = fkc.referenced_object_id
    AND principalColumn.column_id = fkc.referenced_column_id
WHERE fkc.referenced_object_id = OBJECT_ID(N'{principalTable}')
    AND principalColumn.name = N'{principalKeyColumn}';
EXEC sys.sp_executesql @statement,
    N'@target uniqueidentifier, @source uniqueidentifier',
    @target = @target, @source = @source;";
        return db.Database.ExecuteSqlRawAsync(sql, targetId, sourceId);
    }

    private static string NormalizePersonName(string? value) =>
        (value ?? string.Empty).Trim().Normalize(System.Text.NormalizationForm.FormKC).ToUpperInvariant();



    private UserAccountEntity CreateAccount(Guid personId, DateTime now) => new()
    {
        AccountId = Guid.NewGuid(), PersonId = personId, AccountStatus = 0,
        MustChangePin = true, SecurityStamp = Guid.NewGuid().ToString("N"), CreatedAtUtc = now
    };

    private static bool IsValidPin(string? pin) =>
        !string.IsNullOrWhiteSpace(pin) && pin.Length == 4 && pin.All(char.IsDigit)
        && !string.Equals(pin, "1234", StringComparison.Ordinal);

    private static async Task<List<UserRoleAssignmentEntity>> GetResidentAssignmentsAsync(
        AppDbContext db, IReadOnlyCollection<Guid> accountIds, DateTime now) =>
        await db.UserRoleAssignments.AsNoTracking().Where(item => accountIds.Contains(item.AccountId)
            && item.RoleKey == "resident" && item.RevokedAtUtc == null
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now))
            .ToListAsync();

    private static ResidentEntity Map(
        ApartmentOccupancyEntity occupancy, PersonEntity person, UserAccountEntity? account,
        IReadOnlyCollection<UserRoleAssignmentEntity> assignments, DateTime now)
    {
        var activeAssignment = account is null ? null : assignments.FirstOrDefault(item =>
            item.AccountId == account.AccountId && item.ScopeBuildingId == occupancy.BuildingId
            && (item.ScopeType == 1 || item.ScopeApartmentId == occupancy.ApartmentId));
        var active = account?.AccountStatus == 1 && activeAssignment is not null
            && occupancy.ValidFromUtc <= now && (occupancy.ValidToUtc == null || occupancy.ValidToUtc > now);
        return new ResidentEntity
        {
            Id = occupancy.OccupancyId.ToString("D"),
            Uid = account?.AccountId.ToString("D") ?? "",
            BuildingId = occupancy.BuildingId,
            ApartmentId = occupancy.ApartmentId,
            Name = person.DisplayName,
            Phone = person.PrimaryPhone ?? "",
            Whatsapp = person.Whatsapp ?? "",
            Email = person.PrimaryEmail ?? "",
            PinHash = "",
            IsPrimary = occupancy.IsPrimary,
            IsOwner = occupancy.OccupancyType == 1,
            IsActive = active,
            IsDisabled = !active,
            DisabledReason = active ? "" : activeAssignment?.RevocationReason ?? "Access is not active",
            CreatedAt = occupancy.RecordedAtUtc,
            CreatedBy = occupancy.RecordedByAccountId?.ToString("D") ?? "system",
            UpdatedAt = person.UpdatedAtUtc,
            UpdatedBy = null
        };
    }

    private static void AddAudit(AppDbContext db, Guid? actorId, string? actorReference,
        string buildingId, string apartmentId, string eventType, object details, DateTime now)
    {
        db.AuditEvents.Add(new AuditEventEntity
        {
            AuditEventId = Guid.NewGuid(), OccurredAtUtc = now, ActorAccountId = actorId,
            ActorReference = actorReference, BuildingId = buildingId, EventCategory = "ResidentManagement",
            EventType = eventType, ResourceType = "ApartmentOccupancy", ResourceReference = apartmentId,
            Outcome = 0, SafeDetailsJson = System.Text.Json.JsonSerializer.Serialize(details)
        });
    }
}
