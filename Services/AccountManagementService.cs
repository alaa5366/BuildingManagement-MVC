using BuildingManagementMvc.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Read-only inventory over canonical target identity tables.</summary>
public sealed class AccountManagementService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public AccountManagementService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

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
            }
            else
            {
                row.Roles = "بدون دور نشط";
                row.RoleContexts = "لا يوجد نطاق صلاحية نشط";
            }
            row.IsDuplicatePhone = duplicateIds.Contains(row.PersonId);
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
            AccountsWithoutActiveRole = rows.Count(x => x.ActiveRoleCount == 0),
            Rows = result.Skip((page - 1) * pageSize).Take(pageSize).ToList()
        };
    }
}

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
    public int AccountsWithoutActiveRole { get; init; }
    public List<AccountManagementRow> Rows { get; init; } = new();
}

public sealed class AccountManagementRow
{
    public Guid AccountId { get; set; }
    public Guid PersonId { get; set; }
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
