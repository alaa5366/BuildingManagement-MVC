using System.Data;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Produces a safe resident-account readiness report and provisions eligible accounts
/// in small, repeatable batches. Existing accounts and credentials are never modified.
/// </summary>
public sealed class ResidentAccountProvisioningService
{
    private const byte ActiveAccountStatus = 1;
    private const byte PasswordCredentialType = 0;
    private const int MaximumBatchSize = 50;
    private const string InitialPin = "1234";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogger _audit;
    private readonly ILogger<ResidentAccountProvisioningService> _logger;

    public ResidentAccountProvisioningService(
        IDbContextFactory<AppDbContext> dbFactory,
        IPasswordHasher hasher,
        IAuditLogger audit,
        ILogger<ResidentAccountProvisioningService> logger)
    {
        _dbFactory = dbFactory;
        _hasher = hasher;
        _audit = audit;
        _logger = logger;
    }

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
        var phoneCounts = people
            .Select(x => new { x.PersonId, Normalized = AuthHelpers.NormalizePhone(x.PrimaryPhone) })
            .Where(x => IsPlausibleE164(x.Normalized))
            .GroupBy(x => x.Normalized!, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

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
                reason = account.AccountStatus == ActiveAccountStatus
                    ? (account.MustChangePin ? "Existing account; PIN change required" : "Existing account")
                    : "Existing account is inactive; review manually";
            }
            else if (string.IsNullOrWhiteSpace(person.PrimaryPhone))
            {
                status = "MissingPhone";
                reason = "No primary phone number";
            }
            else if (!IsPlausibleE164(normalizedPhone))
            {
                status = "InvalidPhone";
                reason = "Primary phone cannot be normalized";
            }
            else if (!phoneCounts.TryGetValue(normalizedPhone, out var count) || count != 1)
            {
                status = "DuplicatePhone";
                reason = "Primary phone is shared by more than one person record";
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
            else
            {
                status = "Eligible";
                reason = "Ready for secure account creation";
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
            ActiveAccounts = accounts.Count(x => x.AccountStatus == ActiveAccountStatus),
            NonActiveAccounts = accounts.Count(x => x.AccountStatus != ActiveAccountStatus),
            AccountsRequiringPinChange = accounts.Count(x => x.MustChangePin),
            Eligible = rows.Count(x => x.Status == "Eligible"),
            MissingPhone = rows.Count(x => x.Status == "MissingPhone"),
            InvalidPhone = rows.Count(x => x.Status == "InvalidPhone"),
            DuplicatePhone = rows.Count(x => x.Status == "DuplicatePhone"),
            NoActiveOccupancy = rows.Count(x => x.Status == "NoActiveOccupancy"),
            ExistingAccounts = rows.Count(x => x.Status == "Existing"),
            RoleUnavailable = rows.Count(x => x.Status == "RoleUnavailable"),
            Rows = rows.OrderBy(x => x.Status == "Eligible" ? 0 : 1)
                .ThenBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Take(500)
                .ToList()
        };
    }

    public async Task<ResidentAccountProvisioningBatchResult> ProvisionBatchAsync(
        int requestedBatchSize,
        string actorId,
        CancellationToken cancellationToken = default)
    {
        var batchSize = Math.Clamp(requestedBatchSize, 1, MaximumBatchSize);
        var report = await GetReportAsync(cancellationToken);
        var candidates = report.Rows
            .Where(x => x.Status == "Eligible")
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
                action: "resident.account.provisioned",
                userId: actorId,
                userRole: "superadmin",
                metadata: new { personReference = candidate.PersonId, initialPinRequiresChange = true });
        }

        result.RemainingEligible = (await GetReportAsync(cancellationToken)).Eligible;
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

            var normalizedPhone = AuthHelpers.NormalizePhone(person.PrimaryPhone);
            if (!IsPlausibleE164(normalizedPhone))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var phoneMatches = people.Count(x => string.Equals(
                AuthHelpers.NormalizePhone(x.PrimaryPhone), normalizedPhone, StringComparison.Ordinal));
            if (phoneMatches != 1)
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
                AccountStatus = ActiveAccountStatus,
                MustChangePin = true,
                NormalizedEmail = string.IsNullOrWhiteSpace(person.PrimaryEmail)
                    ? null
                    : person.PrimaryEmail.Trim().ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = now
            });
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = Guid.NewGuid(),
                AccountId = accountId,
                CredentialType = PasswordCredentialType,
                CredentialHash = _hasher.HashPin(InitialPin),
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
    public int MissingPhone { get; init; }
    public int InvalidPhone { get; init; }
    public int DuplicatePhone { get; init; }
    public int NoActiveOccupancy { get; init; }
    public int ExistingAccounts { get; init; }
    public int RoleUnavailable { get; init; }
    public List<ResidentAccountProvisioningRow> Rows { get; init; } = new();
}

public sealed record ResidentAccountProvisioningRow(
    Guid PersonId,
    string DisplayName,
    string Phone,
    string Status,
    string Reason);

public sealed class ResidentAccountProvisioningBatchResult
{
    public int Requested { get; set; }
    public int Created { get; set; }
    public int Skipped { get; set; }
    public int RemainingEligible { get; set; }
}
