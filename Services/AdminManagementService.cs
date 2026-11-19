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
            where assignment.RoleKey == "admin" && assignment.ScopeType == 1
                && assignment.RevokedAtUtc == null
                && (assignment.ValidFromUtc <= now)
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                && (buildingId == null || assignment.ScopeBuildingId == buildingId)
            select new { Account = account, Person = person, assignment.ScopeBuildingId })
            .ToListAsync();

        var result = new List<AppUserDoc>();
        foreach (var group in rows.GroupBy(x => x.Account.AccountId))
        {
            var first = group.First();
            var buildings = group.Select(x => x.ScopeBuildingId).Where(x => x != null)
                .Cast<string>().Distinct(StringComparer.Ordinal).ToList();
            var permissions = await GetEffectivePermissionsAsync(db, first.Account.AccountId, buildings, now);
            result.Add(Map(first.Account, first.Person, buildings, permissions));
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

        var buildings = await db.UserRoleAssignments.AsNoTracking()
            .Where(item => item.AccountId == accountId && item.RoleKey == "admin"
                && item.ScopeType == 1 && item.RevokedAtUtc == null
                && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now))
            .Select(item => item.ScopeBuildingId!).Distinct().ToListAsync();
        if (buildings.Count == 0) return null;
        var permissions = await GetEffectivePermissionsAsync(db, accountId, buildings, now);
        return Map(record.Account, record.Person, buildings, permissions);
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
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var grantedBy = Guid.TryParse(createdBy, out var actorId) ? actorId : (Guid?)null;
        var selectedPermissions = NormalizePermissions(permissions);

        await using var db = await _factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var buildingExists = await db.Buildings.AnyAsync(item => item.Id == buildingId);
        if (!buildingExists) throw new InvalidOperationException("The selected building does not exist.");

        var duplicatePhone = await db.Persons.AsNoTracking().Where(item => item.PrimaryPhone != null)
            .Select(item => item.PrimaryPhone).ToListAsync();
        if (duplicatePhone.Any(value => string.Equals(AuthHelpers.NormalizePhone(value), normalizedPhone, StringComparison.Ordinal)))
            throw new InvalidOperationException("This phone number is already linked to another person.");

        var person = new PersonEntity
        {
            PersonId = personId,
            DisplayName = displayName,
            PrimaryPhone = normalizedPhone,
            Whatsapp = normalizedPhone,
            CreatedAtUtc = now
        };
        var account = new UserAccountEntity
        {
            AccountId = accountId,
            PersonId = personId,
            AccountStatus = 1,
            MustChangePin = false,
            NormalizedEmail = AuthHelpers.AdminEmailForPhone(buildingId, normalizedPhone).ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = now
        };
        db.Persons.Add(person);
        db.UserAccounts.Add(account);
        db.UserCredentials.Add(new UserCredentialEntity
        {
            CredentialId = Guid.NewGuid(),
            AccountId = accountId,
            CredentialType = 0,
            CredentialHash = _hasher.HashPin(pin),
            HashAlgorithm = "BCrypt.Net-Next",
            CreatedAtUtc = now
        });
        db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
        {
            RoleAssignmentId = Guid.NewGuid(),
            AccountId = accountId,
            RoleKey = "admin",
            ScopeType = 1,
            ScopeBuildingId = buildingId,
            ValidFromUtc = now,
            GrantedAtUtc = now,
            GrantedByAccountId = grantedBy,
            GrantReason = "Administrator account created"
        });
        AddPermissionOverrides(db, accountId, new[] { buildingId }, selectedPermissions, grantedBy, now);
        db.AuditEvents.Add(CreateAudit(grantedBy, createdBy, buildingId, "admin.create",
            accountId.ToString("D"), new { accountId, personId, phone = normalizedPhone, permissions = selectedPermissions }, now));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return accountId.ToString("D");
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
            && item.RoleKey == "admin" && item.ScopeType == 1 && item.RevokedAtUtc == null
            && item.ValidFromUtc <= now && (item.ValidToUtc == null || item.ValidToUtc > now)).ToListAsync();
        if (assignments.Count == 0) throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));

        var duplicatePhone = await db.Persons.AsNoTracking()
            .Where(item => item.PersonId != person.PersonId && item.PrimaryPhone != null)
            .Select(item => item.PrimaryPhone).ToListAsync();
        if (duplicatePhone.Any(value => string.Equals(AuthHelpers.NormalizePhone(value), normalizedPhone, StringComparison.Ordinal)))
            throw new InvalidOperationException("This phone number is already linked to another person.");

        person.DisplayName = displayName;
        person.PrimaryPhone = normalizedPhone;
        person.Whatsapp = normalizedPhone;
        person.UpdatedAtUtc = now;
        account.AccountStatus = isActive ? (byte)1 : (byte)0;
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
        account.AccountStatus = activate ? (byte)1 : (byte)0;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        db.AuditEvents.Add(CreateAudit(Guid.TryParse(updatedBy, out var actorId) ? actorId : null,
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
            && item.RoleKey == "admin" && item.RevokedAtUtc == null).ToListAsync();
        if (assignments.Count == 0) throw new InvalidOperationException(Loc.T("The_Admin_Does_Not_Exist"));
        foreach (var assignment in assignments)
        {
            assignment.RevokedAtUtc = now;
            assignment.RevokedByAccountId = actorId;
            assignment.RevocationReason = "Administrator access removed";
        }
        var hasOtherActiveRole = await db.UserRoleAssignments.AnyAsync(item => item.AccountId == accountId
            && item.RoleKey != "admin" && item.RevokedAtUtc == null
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
            && item.RoleKey == "admin" && item.RevokedAtUtc == null && item.ScopeBuildingId != null)
            .Select(item => item.ScopeBuildingId!).Distinct().ToArrayAsync();
        if (buildings.Length == 0) return;
        AddPermissionOverrides(db, accountId, buildings, NormalizePermissions(permissions), actorId, now);
        db.AuditEvents.Add(CreateAudit(actorId, updatedBy, buildings.FirstOrDefault(), "admin.sync_permissions",
            accountId.ToString("D"), new { accountId, count = permissions?.Count ?? 0 }, now));
        await db.SaveChangesAsync();
        _sessionRevalidator?.InvalidateAdmin(uid);
    }

    private static AppUserDoc Map(UserAccountEntity account, PersonEntity person,
        List<string> buildings, List<string> permissions) => new()
    {
        Uid = account.AccountId.ToString("D"),
        Email = account.NormalizedEmail ?? person.PrimaryEmail ?? "",
        Name = person.DisplayName,
        Phone = person.PrimaryPhone ?? "",
        Whatsapp = person.Whatsapp ?? "",
        PhotoUrl = person.PhotoUrl ?? "",
        Role = "admin",
        BuildingIds = buildings,
        Disabled = account.AccountStatus != 1,
        IsActive = account.AccountStatus == 1,
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
            .Where(item => item.RoleKey == "admin" && item.Effect == 1)
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
