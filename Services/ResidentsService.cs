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
    private readonly ILogger<ResidentsService> _logger;

    public ResidentsService(
        IDbContextFactory<AppDbContext> factory,
        IAuditLogger audit,
        ILogger<ResidentsService> logger)
    {
        _factory = factory;
        _audit = audit;
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
    /// <remarks>Phone numbers are contact attributes, never identity keys. A newly created login is pending and has no credential.</remarks>
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

    /// <summary> Performs the MigrateFromApartmentsAsync operation. </summary>
    public Task<MigrationResult> MigrateFromApartmentsAsync(string? buildingId, bool dryRun) =>
        Task.FromResult(new MigrationResult());

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
