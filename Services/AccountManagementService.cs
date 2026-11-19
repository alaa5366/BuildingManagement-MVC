using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Read-only inventory over canonical target identity tables.</summary>
public sealed class AccountManagementService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditLogger _audit;
    public AccountManagementService(IDbContextFactory<AppDbContext> factory, IAuditLogger audit) { _factory = factory; _audit = audit; }

    /// <summary>Retrieves a paginated inventory of canonical identity accounts.</summary>
    public async Task<AccountManagementPage> GetPageAsync(string? search, string? role, string? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var rows = await (from account in db.UserAccounts.AsNoTracking()
                          join person in db.Persons.AsNoTracking() on account.PersonId equals person.PersonId
                          select new AccountManagementRow
                          {
                              AccountId = account.AccountId, PersonId = person.PersonId,
                              Name = person.DisplayName, Phone = person.PrimaryPhone,
                              Email = account.NormalizedEmail ?? person.PrimaryEmail,
                              AccountStatus = account.AccountStatus, MustChangePin = account.MustChangePin,
                              IsArchived = person.IsArchived, LastLoginAtUtc = account.LastLoginAtUtc
                          }).ToListAsync(ct);

        var grants = await (from grant in db.UserRoleAssignments.AsNoTracking()
                            join definition in db.RoleDefinitions.AsNoTracking() on grant.RoleKey equals definition.RoleKey
                            where grant.ValidFromUtc <= now && (grant.ValidToUtc == null || grant.ValidToUtc > now)
                                && grant.RevokedAtUtc == null && definition.IsActive
                            select new { grant.AccountId, grant.RoleKey, grant.ScopeType, grant.ScopeBuildingId, grant.ScopeApartmentId })
                           .ToListAsync(ct);
        var byAccount = grants.GroupBy(x => x.AccountId).ToDictionary(g => g.Key, g => g.ToList());
        var protectedSuperAdminAccountIds = (await db.UserRoleAssignments.AsNoTracking()
            .Where(x => x.RoleKey == "superadmin" && x.RevokedAtUtc == null)
            .Select(x => x.AccountId).Distinct().ToListAsync(ct)).ToHashSet();

        // Include archived persons in duplicate detection to avoid implying a retained number is unique.
        var people = await db.Persons.AsNoTracking().Select(x => new { x.PersonId, x.PrimaryPhone }).ToListAsync(ct);
        var duplicateIds = people.Select(x => new { x.PersonId, Phone = AuthHelpers.NormalizePhone(x.PrimaryPhone) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Phone))
            .GroupBy(x => x.Phone!, StringComparer.Ordinal).Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(x => x.PersonId)).ToHashSet();

        foreach (var row in rows)
        {
            
            if (byAccount.TryGetValue(row.AccountId, out var assignments))
            {
                row.Roles = string.Join(", ", assignments.Select(x => x.RoleKey).Distinct().OrderBy(x => x));
                row.RoleContexts = string.Join(" | ", assignments.Select(x =>
                    $"{x.RoleKey}: {(x.ScopeType switch { 0 => "النظام", 1 => $"عمارة {x.ScopeBuildingId ?? "—"}", 2 => $"عمارة {x.ScopeBuildingId ?? "—"} / شقة {x.ScopeApartmentId ?? "—"}", 3 => "شخصي", _ => "نطاق غير معروف" })}").Distinct());
                row.ActiveRoleCount = assignments.Count;
                if (assignments.Any(x => x.RoleKey == "superadmin"))
                {
                    row.Phone = null;
                    row.IsDuplicatePhone = false;
                }
            }
            else
            {
                row.Roles = "بدون دور نشط";
                row.RoleContexts = "لا يوجد نطاق صلاحية نشط";
            }
            var isSuperAdminIdentity = protectedSuperAdminAccountIds.Contains(row.AccountId)
                || row.Roles.Split(", ", StringSplitOptions.RemoveEmptyEntries)
                    .Contains("superadmin", StringComparer.OrdinalIgnoreCase);
            if (isSuperAdminIdentity) row.Phone = null;
            row.IsDuplicatePhone = !isSuperAdminIdentity && duplicateIds.Contains(row.PersonId);
            row.StatusLabel = row.AccountStatus switch
            {
                1 => "نشط", 0 => "غير نشط", 2 => "موقوف / مقفل", 3 => "معلق", _ => "غير معروف"
            };
        }

        IEnumerable<AccountManagementRow> query = rows;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (x.Phone?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || x.AccountId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.PersonId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(x => x.Roles.Split(", ", StringSplitOptions.RemoveEmptyEntries).Contains(role, StringComparer.OrdinalIgnoreCase));
        if (status == "active") query = query.Where(x => x.AccountStatus == 1);
        else if (status == "inactive") query = query.Where(x => x.AccountStatus != 1);

        var result = query.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        pageSize = Math.Clamp(pageSize, 10, 100);
        var total = result.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        return new AccountManagementPage
        {
            Search = search, Role = role, Status = status, Page = page, PageSize = pageSize,
            Total = total, TotalPages = totalPages, TotalAccounts = rows.Count,
            ActiveAccounts = rows.Count(x => x.AccountStatus == 1),
            InactiveAccounts = rows.Count(x => x.AccountStatus != 1),
            DuplicatePhoneAccounts = rows.Count(x => x.IsDuplicatePhone),
            LegacySuperAdminAccounts = 0,
            AccountsWithoutActiveRole = rows.Count(x => x.ActiveRoleCount == 0),
            Rows = result.Skip((page - 1) * pageSize).Take(pageSize).ToList()
        };
    }
    /// <summary>Updates the status of a resident account.</summary>
    public async Task<AccountManagementMutationResult> SetResidentStatusAsync(Guid accountId, bool activate, string actorId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.AccountId == accountId, ct);
        if (account is null) return AccountManagementMutationResult.Fail("الحساب غير موجود.");
        var personArchived = await db.Persons.AnyAsync(x => x.PersonId == account.PersonId && x.IsArchived, ct);
        if (activate && personArchived)
            return AccountManagementMutationResult.Fail("لا يمكن تفعيل حساب شخص مؤرشف.");
        var now = DateTime.UtcNow;
        var hasResidentRole = await (
            from assignment in db.UserRoleAssignments
            join definition in db.RoleDefinitions on assignment.RoleKey equals definition.RoleKey
            where assignment.AccountId == accountId && assignment.RoleKey == "resident"
                && definition.IsActive && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                && assignment.RevokedAtUtc == null
            select assignment.RoleAssignmentId).AnyAsync(ct);
        var hasAdminRole = await db.UserRoleAssignments.AnyAsync(x => x.AccountId == accountId
            && x.RoleKey == "admin" && x.ValidFromUtc <= now
            && (x.ValidToUtc == null || x.ValidToUtc > now) && x.RevokedAtUtc == null, ct);
        var hasSuperAdminRole = await db.UserRoleAssignments.AnyAsync(x => x.AccountId == accountId
            && x.RoleKey == "superadmin" && x.RevokedAtUtc == null, ct);
        if (!hasResidentRole || hasAdminRole || hasSuperAdminRole)
            return AccountManagementMutationResult.Fail("إدارة الحالة من هذه الشاشة متاحة فقط لحساب resident مستقل. حسابات الإدمن وSuperAdmin تُدار من مساراتها المختصة.");
        if (account.AccountStatus is not (0 or 1))
            return AccountManagementMutationResult.Fail("الحساب موقوف/معلق بحالة خاصة؛ لا تغيّر حالته من هذه الشاشة.");
        var next = activate ? (byte)1 : (byte)0;
        if (account.AccountStatus == next)
            return AccountManagementMutationResult.Ok("الحساب بالفعل على الحالة المطلوبة.");
        account.AccountStatus = next;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.LogAsync(activate ? "identity.resident.account.activated" : "identity.resident.account.deactivated", userId: actorId, userRole: "superadmin", metadata: new { AccountId = accountId });
        return AccountManagementMutationResult.Ok(activate ? "تم تفعيل حساب الساكن." : "تم إيقاف حساب الساكن.");
    }

    /// <summary>Resets the PIN for a resident account.</summary>
    public async Task<AccountManagementMutationResult> ResetResidentPinAsync(Guid accountId, string actorId, IPasswordHasher hasher, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.AccountId == accountId, ct);
        if (account is null) return AccountManagementMutationResult.Fail("الحساب غير موجود.");
        if (await db.Persons.AnyAsync(x => x.PersonId == account.PersonId && x.IsArchived, ct))
            return AccountManagementMutationResult.Fail("لا يمكن إعادة تعيين PIN لشخص مؤرشف.");
        var now = DateTime.UtcNow;
        var hasResidentRole = await (
            from assignment in db.UserRoleAssignments
            join definition in db.RoleDefinitions on assignment.RoleKey equals definition.RoleKey
            where assignment.AccountId == accountId && assignment.RoleKey == "resident"
                && definition.IsActive && assignment.ValidFromUtc <= now
                && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                && assignment.RevokedAtUtc == null
            select assignment.RoleAssignmentId).AnyAsync(ct);
        var hasAdminRole = await db.UserRoleAssignments.AnyAsync(x => x.AccountId == accountId
            && x.RoleKey == "admin" && x.ValidFromUtc <= now
            && (x.ValidToUtc == null || x.ValidToUtc > now) && x.RevokedAtUtc == null, ct);
        var hasSuperAdminRole = await db.UserRoleAssignments.AnyAsync(x => x.AccountId == accountId
            && x.RoleKey == "superadmin" && x.RevokedAtUtc == null, ct);
        if (!hasResidentRole || hasAdminRole || hasSuperAdminRole)
            return AccountManagementMutationResult.Fail("إعادة تعيين PIN من هذه الشاشة متاحة فقط لحساب resident مستقل.");
        if (account.AccountStatus != 1)
            return AccountManagementMutationResult.Fail("فعّل الحساب أولًا قبل إعادة تعيين PIN.");
        var pin = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var activeCredentials = await db.UserCredentials.Where(x => x.AccountId == accountId
            && x.CredentialType == 0 && x.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var credential in activeCredentials) credential.RevokedAtUtc = now;
        db.UserCredentials.Add(new BuildingManagementMvc.Data.Entities.UserCredentialEntity
        {
            CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0,
            CredentialHash = hasher.HashPin(pin), HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
        });
        account.MustChangePin = true;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.LogAsync("identity.resident.pin.reset", userId: actorId, userRole: "superadmin", metadata: new { AccountId = accountId, MustChangePin = true });
        return AccountManagementMutationResult.Ok("تم إصدار PIN مؤقت. اعرضه للمستخدم مرة واحدة واطلب تغييره عند أول دخول.", pin);
    }

    // Admin and resident accounts share mobile/PIN authentication. Only a single distinct
    // managed role is eligible; multiple building scopes for that same role remain valid.
    private async Task<string?> GetSoleManagedRoleAsync(AppDbContext db, Guid accountId, DateTime now, CancellationToken ct)
    {
        var roles = await (from assignment in db.UserRoleAssignments
                           join definition in db.RoleDefinitions on assignment.RoleKey equals definition.RoleKey
                           where assignment.AccountId == accountId && definition.IsActive
                               && assignment.ValidFromUtc <= now
                               && (assignment.ValidToUtc == null || assignment.ValidToUtc > now)
                               && assignment.RevokedAtUtc == null
                           select assignment.RoleKey).Distinct().ToListAsync(ct);
        return roles.Count == 1 && (roles[0] == "resident" || roles[0] == "admin" || roles[0] == "building-admin") ? roles[0] : null;
    }

    /// <summary>Updates the status of a managed account.</summary>
    public async Task<AccountManagementMutationResult> SetManagedAccountStatusAsync(
        Guid accountId, bool activate, string actorId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.AccountId == accountId, ct);
        if (account is null) return AccountManagementMutationResult.Fail("الحساب غير موجود.");
        var now = DateTime.UtcNow;
        var role = await GetSoleManagedRoleAsync(db, accountId, now, ct);
        if (role is null)
            return AccountManagementMutationResult.Fail("يمكن إدارة حساب resident أو admin بدور واحد فقط. الحسابات متعددة الأدوار وSuperAdmin محمية.");
        if (activate && await db.Persons.AnyAsync(x => x.PersonId == account.PersonId && x.IsArchived, ct))
            return AccountManagementMutationResult.Fail("لا يمكن تفعيل حساب شخص مؤرشف.");
        if (account.AccountStatus is not (0 or 1))
            return AccountManagementMutationResult.Fail("الحساب موقوف/معلق بحالة خاصة؛ لا تغيّر حالته من هذه الشاشة.");
        var next = activate ? (byte)1 : (byte)0;
        if (account.AccountStatus == next)
            return AccountManagementMutationResult.Ok("الحساب بالفعل على الحالة المطلوبة.");
        account.AccountStatus = next;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.LogAsync(activate ? $"identity.{role}.account.activated" : $"identity.{role}.account.deactivated",
            userId: actorId, userRole: "superadmin", metadata: new { AccountId = accountId, Role = role });
        return AccountManagementMutationResult.Ok(activate ? "تم تفعيل الحساب." : "تم إيقاف الحساب.");
    }

    /// <summary>Resets the PIN for a managed account.</summary>
    public async Task<AccountManagementMutationResult> ResetManagedAccountPinAsync(
        Guid accountId, string actorId, IPasswordHasher hasher, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.AccountId == accountId, ct);
        if (account is null) return AccountManagementMutationResult.Fail("الحساب غير موجود.");
        if (await db.Persons.AnyAsync(x => x.PersonId == account.PersonId && x.IsArchived, ct))
            return AccountManagementMutationResult.Fail("لا يمكن إعادة تعيين PIN لشخص مؤرشف.");
        var now = DateTime.UtcNow;
        var role = await GetSoleManagedRoleAsync(db, accountId, now, ct);
        if (role is null)
            return AccountManagementMutationResult.Fail("يمكن إعادة تعيين PIN لحساب resident أو admin بدور واحد فقط. الحسابات متعددة الأدوار وSuperAdmin محمية.");
        if (account.AccountStatus != 1)
            return AccountManagementMutationResult.Fail("فعّل الحساب أولًا قبل إعادة تعيين PIN.");
        var pin = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var activeCredentials = await db.UserCredentials.Where(x => x.AccountId == accountId
            && x.CredentialType == 0 && x.RevokedAtUtc == null).ToListAsync(ct);
        foreach (var credential in activeCredentials) credential.RevokedAtUtc = now;
        db.UserCredentials.Add(new BuildingManagementMvc.Data.Entities.UserCredentialEntity
        {
            CredentialId = Guid.NewGuid(), AccountId = accountId, CredentialType = 0,
            CredentialHash = hasher.HashPin(pin), HashAlgorithm = "BCrypt.Net-Next", CreatedAtUtc = now
        });
        account.MustChangePin = true;
        account.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.LogAsync($"identity.{role}.pin.reset", userId: actorId, userRole: "superadmin",
            metadata: new { AccountId = accountId, Role = role, MustChangePin = true });
        return AccountManagementMutationResult.Ok("تم إصدار PIN مؤقت. اعرضه للمستخدم مرة واحدة واطلب تغييره عند أول دخول.", pin);
    }

}

/// <summary>AccountManagementPage coordinates the corresponding application functionality.</summary>
public sealed class AccountManagementPage
{
    public string? Search { get; init; }
    public string? Role { get; init; }
    public string? Status { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
    public int TotalAccounts { get; init; }
    public int ActiveAccounts { get; init; }
    public int InactiveAccounts { get; init; }
    public int DuplicatePhoneAccounts { get; init; }
    public int LegacySuperAdminAccounts { get; init; }
    public int AccountsWithoutActiveRole { get; init; }
    public List<AccountManagementRow> Rows { get; init; } = new();
}

/// <summary>AccountManagementRow coordinates the corresponding application functionality.</summary>
public sealed class AccountManagementRow
{
    public Guid AccountId { get; set; }
    public Guid PersonId { get; set; }
    public string? LegacyUserId { get; set; }
    public bool IsLegacyAccount { get; set; }
    public bool IsSuperAdminEmailAllowed { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public byte AccountStatus { get; set; }
    public bool MustChangePin { get; set; }
    public bool IsArchived { get; set; }
    public bool IsDuplicatePhone { get; set; }
    public string Roles { get; set; } = "";
    public string RoleContexts { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public int ActiveRoleCount { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

}

/// <summary>AccountManagementMutationResult coordinates the corresponding application functionality.</summary>
public sealed record AccountManagementMutationResult(bool Success, string Message, string? TemporaryPin = null, string? TemporaryPassword = null)
{
    /// <summary>Creates a failed account-management operation result.</summary>
    public static AccountManagementMutationResult Fail(string message) => new(false, message);
    /// <summary>Creates a successful account-management operation result.</summary>
    public static AccountManagementMutationResult Ok(string message, string? pin = null, string? password = null) =>
        new(true, message, pin, password);
}
