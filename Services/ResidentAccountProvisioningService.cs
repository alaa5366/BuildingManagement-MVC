using System.Data;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Produces a safe resident-account readiness report and provisions pending accounts
/// in small, repeatable batches. Existing accounts and credentials are never modified.
/// </summary>
public sealed class ResidentAccountProvisioningService
{
    private const byte PendingAccountStatus = 0;
    private const int MaximumBatchSize = 50;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ResidentAccountProvisioningService> _logger;

    public ResidentAccountProvisioningService(
        IDbContextFactory<AppDbContext> dbFactory,
        IAuditLogger audit,
        ILogger<ResidentAccountProvisioningService> logger)
    {
        _dbFactory = dbFactory;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>Builds the resident account provisioning report.</summary>
    public async Task<ResidentAccountProvisioningReport> GetReportAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var people = await db.Persons.AsNoTracking().ToListAsync(cancellationToken);
        var accounts = await db.UserAccounts.AsNoTracking()
            .Select(x => new { x.PersonId, x.AccountStatus, x.MustChangePin })
            .ToListAsync(cancellationToken);
        var occupiedPersonIds = await db.ApartmentOccupancies.AsNoTracking()
            .Where(x => x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now))
            .Select(x => x.PersonId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var activeRoleExists = await db.RoleDefinitions.AsNoTracking()
            .AnyAsync(x => x.RoleKey == "resident" && x.IsActive, cancellationToken);

        var accountByPerson = accounts.GroupBy(x => x.PersonId).ToDictionary(x => x.Key, x => x.First());
        var occupancySet = occupiedPersonIds.ToHashSet();
        var phoneGroups = people
            .Select(x => new { x.PersonId, Normalized = AuthHelpers.NormalizePhone(x.PrimaryPhone) })
            .Where(x => IsPlausibleE164(x.Normalized))
            .GroupBy(x => x.Normalized!, StringComparer.Ordinal)
            .ToList();
        var phoneCounts = phoneGroups.ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
        var duplicatePhonePersonIds = phoneGroups
            .Where(x => x.Count() > 1)
            .SelectMany(x => x.Select(person => person.PersonId))
            .ToHashSet();

        var rows = new List<ResidentAccountProvisioningRow>();
        var activePeople = people.Where(x => !x.IsArchived).ToList();
        foreach (var person in activePeople)
        {
            var normalizedPhone = AuthHelpers.NormalizePhone(person.PrimaryPhone);
            var hasAccount = accountByPerson.ContainsKey(person.PersonId);
            string status;
            string reason;

            if (hasAccount)
            {
                status = "Existing";
                var account = accountByPerson[person.PersonId];
                reason = account.AccountStatus == 0
                    ? "Existing pending account; issue an individual PIN before activation"
                    : account.AccountStatus == 1
                        ? (duplicatePhonePersonIds.Contains(person.PersonId)
                            ? "Existing account; primary phone is duplicated and requires review"
                            : account.MustChangePin ? "Existing account; PIN change required" : "Existing account")
                        : "Existing account is locked or disabled; review manually";
            }
            else if (!occupancySet.Contains(person.PersonId))
            {
                status = "NoActiveOccupancy";
                reason = "No current apartment occupancy";
            }
            else if (!activeRoleExists)
            {
                status = "RoleUnavailable";
                reason = "The resident role is missing or inactive";
            }
            else if (string.IsNullOrWhiteSpace(person.PrimaryPhone))
            {
                status = "MissingPhone";
                reason = "A pending personal account can be created, but a unique phone is required before issuing a PIN";
            }
            else if (!IsPlausibleE164(normalizedPhone))
            {
                status = "InvalidPhone";
                reason = "A pending personal account can be created, but the phone must be corrected before issuing a PIN";
            }
            else if (!phoneCounts.TryGetValue(normalizedPhone, out var count) || count != 1)
            {
                status = "DuplicatePhone";
                reason = "A pending personal account can be created; the shared phone must be resolved before issuing a PIN";
            }
            else
            {
                status = "Eligible";
                reason = "Ready for pending personal account creation; PIN issuance still requires identity verification";
            }

            rows.Add(new ResidentAccountProvisioningRow(
                person.PersonId,
                person.DisplayName,
                person.PrimaryPhone ?? "",
                status,
                reason));
        }

        return new ResidentAccountProvisioningReport
        {
            TotalPersons = people.Count,
            ActivePersons = activePeople.Count,
            ArchivedPersons = people.Count(x => x.IsArchived),
            TotalAccounts = accounts.Count,
            ActiveAccounts = accounts.Count(x => x.AccountStatus == 1),
            NonActiveAccounts = accounts.Count(x => x.AccountStatus != 1),
            AccountsRequiringPinChange = accounts.Count(x => x.MustChangePin),
            Eligible = rows.Count(x => x.Status == "Eligible"),
            Provisionable = rows.Count(x => x.Status is "Eligible" or "MissingPhone" or "InvalidPhone" or "DuplicatePhone"),
            MissingPhone = rows.Count(x => x.Status == "MissingPhone"),
            InvalidPhone = rows.Count(x => x.Status == "InvalidPhone"),
            DuplicatePhone = rows.Count(x => x.Status == "DuplicatePhone"),
            NoActiveOccupancy = rows.Count(x => x.Status == "NoActiveOccupancy"),
            ExistingAccounts = rows.Count(x => x.Status == "Existing"),
            RoleUnavailable = rows.Count(x => x.Status == "RoleUnavailable"),
            Rows = rows.OrderBy(x => x.Status == "Eligible" ? 0
                    : x.Status is "MissingPhone" or "InvalidPhone" or "DuplicatePhone" ? 1 : 2)
                .ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Take(500)
                .ToList()
        };
    }

    /// <summary>Provisions a batch of residents with verified active apartment occupancy.</summary>
    public async Task<ResidentAccountProvisioningBatchResult> ProvisionBatchAsync(
        int requestedBatchSize,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        var batchSize = Math.Clamp(requestedBatchSize, 1, MaximumBatchSize);
        var report = await GetReportAsync(cancellationToken);
        var candidates = report.Rows
            .Where(x => x.Status is "Eligible" or "MissingPhone" or "InvalidPhone" or "DuplicatePhone")
            .Take(batchSize)
            .ToList();

        // A report is advisory. Every candidate is revalidated under a serializable
        // transaction immediately before any account or credential is inserted.
        var result = new ResidentAccountProvisioningBatchResult
        {
            Requested = candidates.Count,
            Skipped = 0,
            Created = 0
        };

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var created = await TryProvisionPersonAsync(candidate.PersonId, cancellationToken);
            if (!created)
            {
                result.Skipped++;
                continue;
            }

            result.Created++;
            await _audit.LogAsync(
                action: "resident.account.provisioned.pending",
                userId: actorId,
                userRole: "superadmin",
                metadata: new { personReference = candidate.PersonId, credentialIssued = false, accountStatus = PendingAccountStatus });
        }

        result.RemainingProvisionable = (await GetReportAsync(cancellationToken)).Provisionable;
        return result;
    }

    private static bool IsPlausibleE164(string value) =>
        value.Length is >= 9 and <= 16 && value[0] == '+' &&
        value.Skip(1).All(char.IsDigit) && value[1] != '0';

    private async Task<bool> TryProvisionPersonAsync(Guid personId, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var people = await db.Persons.ToListAsync(cancellationToken);
            var person = people.SingleOrDefault(x => x.PersonId == personId);
            if (person is null || person.IsArchived || await db.UserAccounts.AnyAsync(
                    x => x.PersonId == personId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var occupancies = await db.ApartmentOccupancies
                .Where(x => x.PersonId == personId
                    && x.ValidFromUtc <= now
                    && (x.ValidToUtc == null || x.ValidToUtc > now))
                .Select(x => new { x.BuildingId, x.ApartmentId })
                .Distinct()
                .ToListAsync(cancellationToken);
            var residentRoleExists = await db.RoleDefinitions.AnyAsync(
                x => x.RoleKey == "resident" && x.IsActive, cancellationToken);
            if (occupancies.Count == 0 || !residentRoleExists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var accountId = Guid.NewGuid();
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = person.PersonId,
                AccountStatus = PendingAccountStatus,
                MustChangePin = true,
                NormalizedEmail = string.IsNullOrWhiteSpace(person.PrimaryEmail)
                    ? null
                    : person.PrimaryEmail.Trim().ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = now
            });
            // No credential is created here. A pending account must receive an
            // individually issued credential through the controlled PIN-reset/enrollment
            // workflow before it can be activated. Never seed a shared/default PIN.

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
                    GrantReason = "Resident account created by the authorized batch provisioning workflow."
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex,
                "Resident account provisioning skipped for person reference {PersonReference} due to a concurrent or relational conflict.",
                personId);
            return false;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>Represents ResidentAccountProvisioningReport, used by the resident account administration workflow.</summary>
public sealed class ResidentAccountProvisioningReport
{
    public int TotalPersons { get; init; }
    public int ActivePersons { get; init; }
    public int ArchivedPersons { get; init; }
    public int TotalAccounts { get; init; }
    public int ActiveAccounts { get; init; }
    public int NonActiveAccounts { get; init; }
    public int AccountsRequiringPinChange { get; init; }
    public int Eligible { get; init; }
    public int Provisionable { get; init; }
    public int MissingPhone { get; init; }
    public int InvalidPhone { get; init; }
    public int DuplicatePhone { get; init; }
    public int NoActiveOccupancy { get; init; }
    public int ExistingAccounts { get; init; }
    public int RoleUnavailable { get; init; }
    public List<ResidentAccountProvisioningRow> Rows { get; init; } = new();
}

/// <summary>Represents ResidentAccountProvisioningRow, used by the resident account administration workflow.</summary>
public sealed record ResidentAccountProvisioningRow(
    Guid PersonId,
    string DisplayName,
    string Phone,
    string Status,
    string Reason);

/// <summary>Represents ResidentAccountProvisioningBatchResult, used by the resident account administration workflow.</summary>
public sealed class ResidentAccountProvisioningBatchResult
{
    public int Requested { get; set; }
    public int Created { get; set; }
    public int Skipped { get; set; }
    public int RemainingProvisionable { get; set; }
}
