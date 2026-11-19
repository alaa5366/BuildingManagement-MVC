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

    /// <summary> Gets or previews data through GetByBuildingAsync. </summary>
    public async Task<List<ResidentEntity>> GetByBuildingAsync(string buildingId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var rows = await (
            from occupancy in db.ApartmentOccupancies.AsNoTracking()
            join person in db.Persons.AsNoTracking() on occupancy.PersonId equals person.PersonId
            join account in db.UserAccounts.AsNoTracking() on person.PersonId equals account.PersonId
            where occupancy.BuildingId == buildingId && !person.IsArchived
            orderby occupancy.ApartmentId, occupancy.IsPrimary descending, person.DisplayName
            select new { Occupancy = occupancy, Person = person, Account = account })
            .ToListAsync();
        var assignments = await GetResidentAssignmentsAsync(db, rows.Select(row => row.Account.AccountId).Distinct().ToList(), now);
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
            join account in db.UserAccounts.AsNoTracking() on person.PersonId equals account.PersonId
            where occupancy.BuildingId == buildingId && occupancy.ApartmentId == apartmentId && !person.IsArchived
            orderby occupancy.IsPrimary descending, person.DisplayName
            select new { Occupancy = occupancy, Person = person, Account = account })
            .ToListAsync();
        var assignments = await GetResidentAssignmentsAsync(db, rows.Select(row => row.Account.AccountId).Distinct().ToList(), now);
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
            join account in db.UserAccounts.AsNoTracking() on person.PersonId equals account.PersonId
            where occupancy.OccupancyId == occupancyId && !person.IsArchived
            select new { Occupancy = occupancy, Person = person, Account = account })
            .SingleOrDefaultAsync();
        if (row is null) return null;
        var assignments = await GetResidentAssignmentsAsync(db, new List<Guid> { row.Account.AccountId }, now);
        return Map(row.Occupancy, row.Person, row.Account, assignments, now);
    }

    /// <summary> Creates or saves data through CreateAsync. </summary>
    public async Task CreateAsync(
        string buildingId, string apartmentId,
        string name, string phone, string whatsapp, string pin, string? email,
        bool isPrimary, bool isOwner, string createdBy)
    {
        var normalizedPhone = AuthHelpers.NormalizePhone(phone);
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 ||
            string.IsNullOrWhiteSpace(normalizedPhone) || !IsValidPin(pin))
            throw new ArgumentException("Valid resident name, phone and PIN are required.");

        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(createdBy, out var parsedActor) ? parsedActor : (Guid?)null;
        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (!await db.Apartments.AnyAsync(item => item.BuildingId == buildingId && item.Id == apartmentId))
            throw new InvalidOperationException("apartment-not-found");

        var matchingPeople = await db.Persons.Where(person => !person.IsArchived && person.PrimaryPhone != null)
            .ToListAsync();
        var personMatches = matchingPeople.Where(person =>
            string.Equals(AuthHelpers.NormalizePhone(person.PrimaryPhone), normalizedPhone, StringComparison.Ordinal)).ToList();
        if (personMatches.Count > 1) throw new InvalidOperationException("resident-phone-ambiguous");

        PersonEntity person;
        UserAccountEntity account;
        var isNewAccount = false;
        if (personMatches.Count == 1)
        {
            person = personMatches[0];
            account = await db.UserAccounts.SingleOrDefaultAsync(item => item.PersonId == person.PersonId)
                ?? CreateAccount(person.PersonId, now);
            if (db.Entry(account).State == EntityState.Detached)
            {
                db.UserAccounts.Add(account);
                isNewAccount = true;
            }
        }
        else
        {
            person = new PersonEntity
            {
                PersonId = Guid.NewGuid(), DisplayName = name.Trim(), PrimaryPhone = normalizedPhone,
                Whatsapp = AuthHelpers.NormalizePhone(string.IsNullOrWhiteSpace(whatsapp) ? normalizedPhone : whatsapp),
                PrimaryEmail = email?.Trim(), CreatedAtUtc = now
            };
            db.Persons.Add(person);
            account = CreateAccount(person.PersonId, now);
            db.UserAccounts.Add(account);
            isNewAccount = true;
        }

        var duplicateOccupancy = await db.ApartmentOccupancies.AnyAsync(item =>
            item.PersonId == person.PersonId && item.BuildingId == buildingId && item.ApartmentId == apartmentId
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now));
        if (duplicateOccupancy) throw new InvalidOperationException("resident-already-linked-to-apartment");

        if (!await db.RoleDefinitions.AnyAsync(role => role.RoleKey == "resident"))
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsSystemRole = true, IsActive = true
            });

        if (isPrimary)
        {
            var existingPrimary = await db.ApartmentOccupancies.Where(item =>
                item.BuildingId == buildingId && item.ApartmentId == apartmentId
                && item.IsPrimary && item.ValidToUtc == null).ToListAsync();
            foreach (var item in existingPrimary) item.IsPrimary = false;
        }

        if (isNewAccount || !await db.UserCredentials.AnyAsync(item =>
                item.AccountId == account.AccountId && item.CredentialType == 0 && item.RevokedAtUtc == null))
        {
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(), AccountId = account.AccountId, CredentialType = 0,
                CredentialHash = _hasher.HashPin(pin), HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
            });
        }

        db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
        {
            RoleAssignmentId = Guid.NewGuid(), AccountId = account.AccountId, RoleKey = "resident",
            ScopeType = 2, ScopeBuildingId = buildingId, ScopeApartmentId = apartmentId,
            ValidFromUtc = now, GrantedAtUtc = now, GrantedByAccountId = actorId,
            GrantReason = "Resident occupancy created"
        });
        db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
        {
            OccupancyId = Guid.NewGuid(), BuildingId = buildingId, ApartmentId = apartmentId,
            PersonId = person.PersonId, OccupancyType = isOwner ? (byte)1 : (byte)2,
            IsPrimary = isPrimary, ValidFromUtc = now, RecordedAtUtc = now, RecordedByAccountId = actorId
        });
        if (personMatches.Count == 1)
        {
            person.DisplayName = name.Trim();
            person.Whatsapp = AuthHelpers.NormalizePhone(string.IsNullOrWhiteSpace(whatsapp) ? normalizedPhone : whatsapp);
            if (!string.IsNullOrWhiteSpace(email)) person.PrimaryEmail = email.Trim();
            person.UpdatedAtUtc = now;
        }
        AddAudit(db, actorId, createdBy, buildingId, apartmentId, "resident.create",
            new { accountId = account.AccountId, personId = person.PersonId }, now);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
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
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2 || string.IsNullOrWhiteSpace(normalizedPhone))
            throw new ArgumentException("Valid resident name and phone are required.");
        var duplicate = await db.Persons.AsNoTracking().AnyAsync(item =>
            item.PersonId != person.PersonId && item.PrimaryPhone == normalizedPhone);
        if (duplicate) throw new InvalidOperationException("resident-phone-already-in-use");

        if (isPrimary)
        {
            var existingPrimary = await db.ApartmentOccupancies.Where(item =>
                item.BuildingId == occupancy.BuildingId && item.ApartmentId == occupancy.ApartmentId
                && item.OccupancyId != occupancyId && item.IsPrimary && item.ValidToUtc == null).ToListAsync();
            foreach (var item in existingPrimary) item.IsPrimary = false;
        }
        person.DisplayName = name.Trim();
        person.PrimaryPhone = normalizedPhone;
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

    /// <summary> Updates account or operational data through ChangePinAsync. </summary>
    public async Task ChangePinAsync(string id, string newPin, string updatedBy)
    {
        if (!Guid.TryParse(id, out var occupancyId) || !IsValidPin(newPin))
            throw new ArgumentException("A valid resident occupancy and four-digit PIN are required.");
        var now = DateTime.UtcNow;
        await using var db = await _factory.CreateDbContextAsync();
        var occupancy = await db.ApartmentOccupancies.SingleOrDefaultAsync(item => item.OccupancyId == occupancyId)
            ?? throw new InvalidOperationException("resident-not-found");
        var accountId = await db.UserAccounts.Where(item => item.PersonId == occupancy.PersonId)
            .Select(item => item.AccountId).SingleOrDefaultAsync();
        if (accountId == Guid.Empty) throw new InvalidOperationException("resident-account-not-found");
        await ReplacePinAsync(db, accountId, newPin, now);
        var account = await db.UserAccounts.SingleAsync(item => item.AccountId == accountId);
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        AddAudit(db, Guid.TryParse(updatedBy, out var actorId) ? actorId : null, updatedBy,
            occupancy.BuildingId, occupancy.ApartmentId, "resident.pin.change", new { occupancyId }, now);
        await db.SaveChangesAsync();
    }

    /// <summary> Updates account or operational data through SetApartmentPinAsync. </summary>
    public async Task SetApartmentPinAsync(string buildingId, string apartmentId, string newPin, string? updatedBy)
    {
        if (!IsValidPin(newPin)) throw new ArgumentException("PIN must contain four digits.");
        var now = DateTime.UtcNow;
        var actorId = Guid.TryParse(updatedBy, out var parsedActor) ? parsedActor : (Guid?)null;
        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var apartment = await db.Apartments.SingleOrDefaultAsync(item =>
            item.BuildingId == buildingId && item.Id == apartmentId)
            ?? throw new InvalidOperationException("apartment-not-found");
        var newAccounts = new Dictionary<Guid, UserAccountEntity>();
        var accountIds = await (
            from occupancy in db.ApartmentOccupancies
            join account in db.UserAccounts on occupancy.PersonId equals account.PersonId
            where occupancy.BuildingId == buildingId && occupancy.ApartmentId == apartmentId
                && occupancy.ValidFromUtc <= now && (occupancy.ValidToUtc == null || occupancy.ValidToUtc > now)
                && account.AccountStatus == 1
            select account.AccountId).Distinct().ToListAsync();
        if (accountIds.Count == 0)
        {
            var phone = AuthHelpers.NormalizePhone(apartment.Phone);
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(apartment.Owner))
                throw new InvalidOperationException("apartment-primary-resident-details-missing");
            var person = new PersonEntity
            {
                PersonId = Guid.NewGuid(), DisplayName = apartment.Owner, PrimaryPhone = phone,
                Whatsapp = phone, PrimaryEmail = apartment.Email, CreatedAtUtc = now
            };
            var account = CreateAccount(person.PersonId, now);
            db.Persons.Add(person);
            db.UserAccounts.Add(account);
            newAccounts[account.AccountId] = account;
            if (!await db.RoleDefinitions.AnyAsync(role => role.RoleKey == "resident"))
                db.RoleDefinitions.Add(new RoleDefinitionEntity
                {
                    RoleKey = "resident", DisplayName = "Resident", IsSystemRole = true, IsActive = true
                });
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), BuildingId = buildingId, ApartmentId = apartmentId,
                PersonId = person.PersonId, OccupancyType = 1, IsPrimary = true,
                ValidFromUtc = now, RecordedAtUtc = now, RecordedByAccountId = actorId
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = account.AccountId, RoleKey = "resident",
                ScopeType = 2, ScopeBuildingId = buildingId, ScopeApartmentId = apartmentId,
                ValidFromUtc = now, GrantedAtUtc = now, GrantedByAccountId = actorId,
                GrantReason = "Primary resident created from apartment profile"
            });
            accountIds.Add(account.AccountId);
        }
        foreach (var accountId in accountIds)
        {
            await ReplacePinAsync(db, accountId, newPin, now);
            if (!newAccounts.TryGetValue(accountId, out var account))
                account = await db.UserAccounts.SingleAsync(item => item.AccountId == accountId);
            account.SecurityStamp = Guid.NewGuid().ToString("N");
        }
        AddAudit(db, actorId, updatedBy, buildingId, apartmentId, "resident.apartment_pin.reset",
            new { affectedAccounts = accountIds.Count }, now);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        _logger.LogInformation("Resident PIN reset for apartment {ApartmentId} in building {BuildingId}; affected {Count} target accounts",
            apartmentId, buildingId, accountIds.Count);
    }

    /// <summary> Updates account or operational data through UpdateResidentPinAsync. </summary>
    public Task UpdateResidentPinAsync(string buildingId, string apartmentId, string newPin) =>
        SetApartmentPinAsync(buildingId, apartmentId, newPin, "system");

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
                item.RevokedByAccountId = actorId;
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
                assignment.RevokedByAccountId = actorId;
                assignment.RevocationReason = "Resident occupancy removed";
            }
            var hasOtherRole = await db.UserRoleAssignments.AnyAsync(item => item.AccountId == accountId
                && item.RevokedAtUtc == null && item.ValidFromUtc <= now
                && (item.ValidToUtc == null || item.ValidToUtc > now));
            if (!hasOtherRole)
            {
                var account = await db.UserAccounts.SingleAsync(item => item.AccountId == accountId);
                account.AccountStatus = 0;
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
        AccountId = Guid.NewGuid(), PersonId = personId, AccountStatus = 1,
        MustChangePin = false, SecurityStamp = Guid.NewGuid().ToString("N"), CreatedAtUtc = now
    };

    private async Task ReplacePinAsync(AppDbContext db, Guid accountId, string pin, DateTime now)
    {
        var old = await db.UserCredentials.Where(item => item.AccountId == accountId
            && item.CredentialType == 0 && item.RevokedAtUtc == null).ToListAsync();
        var replacementId = Guid.NewGuid();
        db.UserCredentials.Add(new UserCredentialEntity
        {
            CredentialId = replacementId, AccountId = accountId, CredentialType = 0,
            CredentialHash = _hasher.HashPin(pin), HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
        });
        foreach (var credential in old)
        {
            credential.RevokedAtUtc = now;
            credential.ReplacedByCredentialId = replacementId;
        }
    }

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
        ApartmentOccupancyEntity occupancy, PersonEntity person, UserAccountEntity account,
        IReadOnlyCollection<UserRoleAssignmentEntity> assignments, DateTime now)
    {
        var activeAssignment = assignments.FirstOrDefault(item =>
            item.AccountId == account.AccountId && item.ScopeBuildingId == occupancy.BuildingId
            && (item.ScopeType == 1 || item.ScopeApartmentId == occupancy.ApartmentId));
        var active = account.AccountStatus == 1 && activeAssignment is not null
            && occupancy.ValidFromUtc <= now && (occupancy.ValidToUtc == null || occupancy.ValidToUtc > now);
        return new ResidentEntity
        {
            Id = occupancy.OccupancyId.ToString("D"),
            Uid = account.AccountId.ToString("D"),
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
