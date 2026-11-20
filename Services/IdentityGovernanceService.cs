using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>SuperAdmin governance operations over canonical identity, occupancy and access-control records.</summary>
public sealed class IdentityGovernanceService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditLogger _audit;

    public IdentityGovernanceService(IDbContextFactory<AppDbContext> factory, IAuditLogger audit)
    {
        _factory = factory;
        _audit = audit;
    }

    public async Task<IdentityGovernancePage> GetPageAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var page = new IdentityGovernancePage
        {
            PersonCount = await db.Persons.CountAsync(ct),
            AccountCount = await db.UserAccounts.CountAsync(ct),
            ActiveOccupancyCount = await db.ApartmentOccupancies.CountAsync(x => x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now), ct),
            ActiveRoleAssignmentCount = await db.UserRoleAssignments.CountAsync(x => x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now) && x.RevokedAtUtc == null, ct),
            PendingPhoneChangeCount = await db.PersonPhoneChangeRequests.CountAsync(x => x.Status == 0, ct)
        };

        page.Persons = await (from p in db.Persons.AsNoTracking()
                              join a in db.UserAccounts.AsNoTracking() on p.PersonId equals a.PersonId into accounts
                              from a in accounts.DefaultIfEmpty()
                              orderby p.DisplayName, p.PersonId
                              select new IdentityGovernancePersonRow
                              {
                                  PersonId = p.PersonId, DisplayName = p.DisplayName, LegalName = p.LegalName,
                                  Phone = p.PrimaryPhone, Email = p.PrimaryEmail, IsArchived = p.IsArchived,
                                  AccountId = a == null ? null : a.AccountId, AccountStatus = a == null ? null : a.AccountStatus,
                                  ActiveOccupancies = db.ApartmentOccupancies.Count(o => o.PersonId == p.PersonId && o.ValidFromUtc <= now && (o.ValidToUtc == null || o.ValidToUtc > now)),
                                  ActiveRoles = a == null ? 0 : db.UserRoleAssignments.Count(r => r.AccountId == a.AccountId && r.ValidFromUtc <= now && (r.ValidToUtc == null || r.ValidToUtc > now) && r.RevokedAtUtc == null)
                              }).Take(200).ToListAsync(ct);

        page.Accounts = await (from a in db.UserAccounts.AsNoTracking()
                               join p in db.Persons.AsNoTracking() on a.PersonId equals p.PersonId
                               orderby p.DisplayName
                               select new IdentityGovernanceAccountRow
                               {
                                   AccountId = a.AccountId, PersonId = p.PersonId, PersonName = p.DisplayName,
                                   Status = a.AccountStatus, MustChangePin = a.MustChangePin, LastLoginAtUtc = a.LastLoginAtUtc,
                                   ActiveCredentials = db.UserCredentials.Count(c => c.AccountId == a.AccountId && c.RevokedAtUtc == null && (c.ExpiresAtUtc == null || c.ExpiresAtUtc > now)),
                                   TwoFactorEnabled = db.UserTwoFactorSettings.Any(t => t.AccountId == a.AccountId && t.IsEnabled),
                                   UnusedRecoveryCodes = db.UserRecoveryCodes.Count(c => c.AccountId == a.AccountId && c.UsedAtUtc == null)
                               }).Take(200).ToListAsync(ct);

        page.Security = await (from a in db.UserAccounts.AsNoTracking()
                               join p in db.Persons.AsNoTracking() on a.PersonId equals p.PersonId
                               orderby p.DisplayName
                               select new IdentityGovernanceSecurityRow
                               {
                                   AccountId = a.AccountId, PersonName = p.DisplayName,
                                   CredentialCount = db.UserCredentials.Count(c => c.AccountId == a.AccountId),
                                   ActiveCredentialCount = db.UserCredentials.Count(c => c.AccountId == a.AccountId && c.RevokedAtUtc == null && (c.ExpiresAtUtc == null || c.ExpiresAtUtc > now)),
                                   TwoFactorEnabled = db.UserTwoFactorSettings.Any(t => t.AccountId == a.AccountId && t.IsEnabled),
                                   RecoveryCodeCount = db.UserRecoveryCodes.Count(c => c.AccountId == a.AccountId),
                                   UnusedRecoveryCodeCount = db.UserRecoveryCodes.Count(c => c.AccountId == a.AccountId && c.UsedAtUtc == null)
                               }).Take(200).ToListAsync(ct);

        page.Occupancies = await (from o in db.ApartmentOccupancies.AsNoTracking()
                                  join p in db.Persons.AsNoTracking() on o.PersonId equals p.PersonId
                                  orderby o.ValidToUtc == null descending, o.ValidFromUtc descending
                                  select new IdentityGovernanceOccupancyRow
                                  {
                                      OccupancyId = o.OccupancyId, PersonId = o.PersonId, PersonName = p.DisplayName,
                                      BuildingId = o.BuildingId, ApartmentId = o.ApartmentId, OccupancyType = o.OccupancyType,
                                      IsPrimary = o.IsPrimary, ValidFromUtc = o.ValidFromUtc, ValidToUtc = o.ValidToUtc,
                                      IsCurrent = o.ValidFromUtc <= now && (o.ValidToUtc == null || o.ValidToUtc > now)
                                  }).Take(300).ToListAsync(ct);

        page.RoleAssignments = await (from r in db.UserRoleAssignments.AsNoTracking()
                                      join a in db.UserAccounts.AsNoTracking() on r.AccountId equals a.AccountId
                                      join p in db.Persons.AsNoTracking() on a.PersonId equals p.PersonId
                                      orderby r.RevokedAtUtc == null descending, r.GrantedAtUtc descending
                                      select new IdentityGovernanceRoleRow
                                      {
                                          AssignmentId = r.RoleAssignmentId, AccountId = r.AccountId, PersonName = p.DisplayName,
                                          RoleKey = r.RoleKey, ScopeType = r.ScopeType, BuildingId = r.ScopeBuildingId,
                                          ApartmentId = r.ScopeApartmentId, ValidFromUtc = r.ValidFromUtc,
                                          ValidToUtc = r.ValidToUtc, RevokedAtUtc = r.RevokedAtUtc, GrantReason = r.GrantReason
                                      }).Take(300).ToListAsync(ct);

        page.RoleCatalog = await db.RoleDefinitions.AsNoTracking().OrderBy(x => x.RoleKey)
            .Select(x => new IdentityGovernanceCatalogRow { Key = x.RoleKey, Title = x.DisplayName, Detail = x.IsSystemRole ? "System role" : "Custom role", Status = x.IsActive ? "Active" : "Inactive" }).ToListAsync(ct);
        page.PermissionCatalog = await db.RolePermissions.AsNoTracking().OrderBy(x => x.RoleKey).ThenBy(x => x.PermissionKey)
            .Select(x => new IdentityGovernanceCatalogRow { Key = x.RoleKey + " / " + x.PermissionKey, Title = x.PermissionKey, Detail = x.RoleKey, Status = x.Effect == 1 ? "Allow" : "Deny" }).Take(500).ToListAsync(ct);
        page.PermissionOverrides = await db.AccountPermissionOverrides.AsNoTracking().OrderByDescending(x => x.GrantedAtUtc)
            .Select(x => new IdentityGovernanceCatalogRow { Key = x.OverrideId.ToString(), Title = x.PermissionKey, Detail = "Account " + x.AccountId + " | scope " + x.ScopeType + " | " + (x.ScopeBuildingId ?? "-") + " / " + (x.ScopeApartmentId ?? "-"), Status = x.RevokedAtUtc == null ? "Active" : "Revoked" }).Take(200).ToListAsync(ct);
        page.ApartmentAccessGrants = await db.ApartmentAccessGrants.AsNoTracking().OrderByDescending(x => x.GrantedAtUtc)
            .Select(x => new IdentityGovernanceCatalogRow { Key = x.GrantId.ToString(), Title = x.GrantType.ToString(), Detail = x.PersonId + " | " + x.BuildingId + " / " + x.ApartmentId + " | " + x.Reason, Status = x.RevokedAtUtc == null ? "Active" : "Revoked" }).Take(200).ToListAsync(ct);
        page.OwnershipRecords = await db.ApartmentOwnerships.AsNoTracking().OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => new IdentityGovernanceCatalogRow { Key = x.OwnershipId.ToString(), Title = x.PersonId.ToString(), Detail = x.BuildingId + " / " + x.ApartmentId + " | " + x.OwnershipSharePercent + "% | " + x.EvidenceReference, Status = x.ValidToUtc == null ? "Open period" : "Historical" }).Take(200).ToListAsync(ct);
        page.LegacyMappings = await db.LegacyIdentityMaps.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new IdentityGovernanceCatalogRow { Key = x.LegacyIdentityMapId.ToString(), Title = x.SourceSystem + " / " + x.SourceTable, Detail = x.SourceKey + " | Person " + (x.PersonId == null ? "unresolved" : x.PersonId.ToString()) + " | Account " + (x.AccountId == null ? "unresolved" : x.AccountId.ToString()), Status = x.MappingStatus.ToString() }).Take(200).ToListAsync(ct);
        page.PhoneChangeRequests = await db.PersonPhoneChangeRequests.AsNoTracking().OrderByDescending(x => x.RequestedAtUtc)
            .Select(x => new IdentityGovernanceCatalogRow { Key = x.RequestId.ToString(), Title = x.RequestedPhone, Detail = x.PreviousPhone + " | " + x.BuildingId + " / " + x.ApartmentId, Status = x.Status.ToString() }).Take(200).ToListAsync(ct);
        page.Buildings = await db.Buildings.AsNoTracking().OrderBy(x => x.BuildingNumber)
            .Select(x => new IdentityGovernanceCatalogRow { Key = x.Id, Title = x.BuildingNumber, Detail = x.Name, Status = "Active" }).ToListAsync(ct);
        page.Apartments = await db.Apartments.AsNoTracking().OrderBy(x => x.BuildingId).ThenBy(x => x.Number)
            .Select(x => new IdentityGovernanceCatalogRow { Key = x.BuildingId + "|" + x.Id, Title = x.Number.ToString(), Detail = x.BuildingId, Status = "Active" }).Take(500).ToListAsync(ct);
        return page;
    }

    public async Task<(bool Success, string Message)> CreatePersonAsync(string displayName, string? legalName, string? phone, string? email, Guid actor, CancellationToken ct)
    {
        displayName = (displayName ?? "").Trim();
        legalName = Clean(legalName, 200); phone = Clean(phone, 32); email = Clean(email, 254);
        if (displayName.Length is < 2 or > 160) return (false, "اسم العرض مطلوب وبحد أقصى 160 حرفًا.");
        await using var db = await _factory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var person = new PersonEntity { PersonId = Guid.NewGuid(), DisplayName = displayName, LegalName = legalName, PrimaryPhone = phone, PrimaryEmail = email, CreatedAtUtc = now };
        db.Persons.Add(person);
        await db.SaveChangesAsync(ct);
        await _audit.LogAsync("identity.person.created", userId: actor.ToString(), userRole: "superadmin", metadata: new { person.PersonId, person.DisplayName, phoneProvided = phone != null, emailProvided = email != null });
        return (true, "تم إنشاء الهوية. لم يتم إنشاء حساب دخول تلقائيًا.");
    }

    public async Task<(bool Success, string Message)> UpdatePersonAsync(Guid personId, string displayName, string? legalName, string? phone, string? email, Guid actor, CancellationToken ct)
    {
        displayName = (displayName ?? "").Trim();
        if (displayName.Length is < 2 or > 160) return (false, "اسم العرض مطلوب وبحد أقصى 160 حرفًا.");
        await using var db = await _factory.CreateDbContextAsync(ct);
        var person = await db.Persons.SingleOrDefaultAsync(x => x.PersonId == personId, ct);
        if (person == null) return (false, "الهوية غير موجودة.");
        if (person.IsArchived) return (false, "لا يمكن تعديل هوية مؤرشفة.");
        person.DisplayName = displayName; person.LegalName = Clean(legalName, 200);
        person.PrimaryPhone = Clean(phone, 32); person.PrimaryEmail = Clean(email, 254); person.UpdatedAtUtc = DateTime.UtcNow;
        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.PersonId == personId, ct);
        if (account != null) account.SecurityStamp = Guid.NewGuid().ToString("N");
        await db.SaveChangesAsync(ct);
        await _audit.LogAsync("identity.person.updated", userId: actor.ToString(), userRole: "superadmin", metadata: new { person.PersonId, phoneChanged = true, accountSessionStampRotated = account != null });
        return (true, "تم تحديث بيانات الهوية وإبطال صلاحية الجلسات القديمة عبر Security Stamp.");
    }

    public async Task<(bool Success, string Message)> SetPersonArchivedAsync(Guid personId, bool archived, Guid actor, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var person = await db.Persons.SingleOrDefaultAsync(x => x.PersonId == personId, ct);
        if (person == null) return (false, "الهوية غير موجودة.");
        if (archived == person.IsArchived) return (true, "الهوية بالفعل على الحالة المطلوبة.");
        if (archived)
        {
            var now = DateTime.UtcNow;
            if (await db.ApartmentOccupancies.AnyAsync(x => x.PersonId == personId && x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now), ct))
                return (false, "يجب إنهاء كل فترات الإشغال النشطة قبل أرشفة الهوية.");
            var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.PersonId == personId, ct);
            if (account != null)
            {
                if (await db.UserRoleAssignments.AnyAsync(x => x.AccountId == account.AccountId && x.RevokedAtUtc == null && (x.ValidToUtc == null || x.ValidToUtc > now), ct))
                    return (false, "يجب إلغاء/إنهاء كل إسنادات الأدوار النشطة قبل أرشفة الهوية.");
                account.AccountStatus = 0;
                account.SecurityStamp = Guid.NewGuid().ToString("N");
            }
            person.ArchivedAtUtc = now;
        }
        else person.ArchivedAtUtc = null;
        person.IsArchived = archived; person.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        await _audit.LogAsync(archived ? "identity.person.archived" : "identity.person.restored", userId: actor.ToString(), userRole: "superadmin", metadata: new { personId });
        return (true, archived ? "تمت أرشفة الهوية دون حذفها." : "تم استعادة الهوية.");
    }

    public async Task<(bool Success, string Message)> CreateOccupancyAsync(Guid personId, string buildingId, string apartmentId, byte occupancyType, bool isPrimary, DateTime validFromUtc, string? notes, Guid actor, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var person = await db.Persons.SingleOrDefaultAsync(x => x.PersonId == personId && !x.IsArchived, ct);
        if (person == null) return (false, "الهوية غير موجودة أو مؤرشفة.");
        if (occupancyType > 3 || string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(apartmentId)) return (false, "بيانات التسكين غير صحيحة.");
        if (!await db.Apartments.AnyAsync(x => x.BuildingId == buildingId && x.Id == apartmentId, ct)) return (false, "الشقة غير موجودة داخل العمارة المحددة.");
        if (await db.ApartmentOccupancies.AnyAsync(x => x.PersonId == personId && x.BuildingId == buildingId && x.ApartmentId == apartmentId && x.ValidFromUtc <= DateTime.UtcNow && (x.ValidToUtc == null || x.ValidToUtc > DateTime.UtcNow), ct))
            return (false, "هذا الشخص لديه بالفعل تسكين نشط في الشقة نفسها.");
        if (isPrimary && await db.ApartmentOccupancies.AnyAsync(x => x.BuildingId == buildingId && x.ApartmentId == apartmentId && x.IsPrimary && (x.ValidToUtc == null || x.ValidToUtc > validFromUtc), ct))
            return (false, "توجد إقامة أساسية متداخلة للشقة. راجع الفترة الحالية أولًا.");
        var now = DateTime.UtcNow;
        var entity = new ApartmentOccupancyEntity { OccupancyId = Guid.NewGuid(), PersonId = personId, BuildingId = buildingId, ApartmentId = apartmentId, OccupancyType = occupancyType, IsPrimary = isPrimary, ValidFromUtc = DateTime.SpecifyKind(validFromUtc, DateTimeKind.Utc), RecordedAtUtc = now, RecordedByAccountId = actor, Notes = Clean(notes, 1000) };
        db.ApartmentOccupancies.Add(entity);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        await _audit.LogAsync("identity.occupancy.created", buildingId: buildingId, apartmentId: apartmentId, userId: actor.ToString(), userRole: "superadmin", metadata: new { entity.OccupancyId, personId });
        return (true, "تم إنشاء سجل التسكين.");
    }

    public async Task<(bool Success, string Message)> EndOccupancyAsync(Guid occupancyId, DateTime validToUtc, Guid actor, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.ApartmentOccupancies.SingleOrDefaultAsync(x => x.OccupancyId == occupancyId, ct);
        if (entity == null) return (false, "سجل التسكين غير موجود.");
        var end = DateTime.SpecifyKind(validToUtc, DateTimeKind.Utc);
        if (end <= entity.ValidFromUtc || (entity.ValidToUtc != null && end > entity.ValidToUtc)) return (false, "تاريخ نهاية الإشغال غير صالح.");
        if (entity.ValidToUtc != null) return (false, "سجل الإشغال منتهٍ بالفعل. لا نعيد كتابة التاريخ التاريخي.");
        entity.ValidToUtc = end;
        var linkedAccount = await db.UserAccounts.SingleOrDefaultAsync(x => x.PersonId == entity.PersonId, ct);
        if (linkedAccount != null)
        {
            var residentAssignments = await db.UserRoleAssignments.Where(x => x.AccountId == linkedAccount.AccountId && x.RoleKey == "resident" && x.ScopeBuildingId == entity.BuildingId && x.ScopeApartmentId == entity.ApartmentId && x.RevokedAtUtc == null && (x.ValidToUtc == null || x.ValidToUtc > end)).ToListAsync(ct);
            foreach (var assignment in residentAssignments)
            {
                assignment.RevokedAtUtc = end; assignment.RevokedByAccountId = actor; assignment.RevocationReason = "Occupancy ended by SuperAdmin governance.";
            }
            linkedAccount.SecurityStamp = Guid.NewGuid().ToString("N");
        }
        await db.SaveChangesAsync(ct);
        await _audit.LogAsync("identity.occupancy.ended", buildingId: entity.BuildingId, apartmentId: entity.ApartmentId, userId: actor.ToString(), userRole: "superadmin", metadata: new { occupancyId, end });
        return (true, "تم إنهاء فترة الإشغال مع الاحتفاظ بالسجل التاريخي.");
    }

    public async Task<(bool Success, string Message)> GrantRoleAsync(Guid accountId, string roleKey, byte scopeType, string? buildingId, string? apartmentId, DateTime validFromUtc, DateTime? validToUtc, string reason, Guid actor, CancellationToken ct)
    {
        roleKey = (roleKey ?? "").Trim(); reason = (reason ?? "").Trim();
        if (roleKey.Length == 0 || reason.Length is < 3 or > 500 || scopeType > 3) return (false, "بيانات الدور أو النطاق أو سبب المنح غير صحيحة.");
        if (roleKey.Equals("superadmin", StringComparison.OrdinalIgnoreCase)) return (false, "منح SuperAdmin لا يتم من مركز الإدارة؛ يخضع لمسار bootstrap المحمي.");
        if ((scopeType is 0 or 3) && (!string.IsNullOrWhiteSpace(buildingId) || !string.IsNullOrWhiteSpace(apartmentId))) return (false, "نطاق النظام/الشخصي لا يقبل عمارة أو شقة.");
        if (scopeType == 1 && (string.IsNullOrWhiteSpace(buildingId) || !string.IsNullOrWhiteSpace(apartmentId))) return (false, "نطاق العمارة يتطلب عمارة فقط.");
        if (scopeType == 2 && (string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(apartmentId))) return (false, "نطاق الشقة يتطلب عمارة وشقة.");
        await using var db = await _factory.CreateDbContextAsync(ct);
        var account = await db.UserAccounts.SingleOrDefaultAsync(x => x.AccountId == accountId, ct);
        if (account == null || await db.Persons.AnyAsync(x => x.PersonId == account.PersonId && x.IsArchived, ct)) return (false, "الحساب غير موجود أو هويته مؤرشفة.");
        if (!await db.RoleDefinitions.AnyAsync(x => x.RoleKey == roleKey && x.IsActive, ct)) return (false, "الدور غير موجود أو غير نشط.");
        if (scopeType >= 1 && !await db.Buildings.AnyAsync(x => x.Id == buildingId, ct)) return (false, "العمارة غير موجودة.");
        if (scopeType == 2 && !await db.Apartments.AnyAsync(x => x.BuildingId == buildingId && x.Id == apartmentId, ct)) return (false, "الشقة غير موجودة داخل العمارة.");
        if (roleKey == "resident" && (scopeType != 2 || !await db.ApartmentOccupancies.AnyAsync(x => x.PersonId == account.PersonId && x.BuildingId == buildingId && x.ApartmentId == apartmentId && x.ValidFromUtc <= DateTime.UtcNow && (x.ValidToUtc == null || x.ValidToUtc > DateTime.UtcNow), ct)))
            return (false, "دور Resident يتطلب نطاق شقة بها إشغال نشط للشخص نفسه.");
        if (roleKey is "admin" or "building-admin" && scopeType != 1) return (false, "دور إدارة العمارة يتطلب نطاق عمارة.");
        var from = DateTime.SpecifyKind(validFromUtc, DateTimeKind.Utc);
        if (validToUtc.HasValue && DateTime.SpecifyKind(validToUtc.Value, DateTimeKind.Utc) <= from) return (false, "تاريخ نهاية الدور يجب أن يكون بعد بدايته.");
        if (await db.UserRoleAssignments.AnyAsync(x => x.AccountId == accountId && x.RoleKey == roleKey && x.ScopeType == scopeType && x.ScopeBuildingId == buildingId && x.ScopeApartmentId == apartmentId && x.RevokedAtUtc == null && (x.ValidToUtc == null || x.ValidToUtc > from) && x.ValidFromUtc <= (validToUtc ?? DateTime.MaxValue), ct))
            return (false, "يوجد إسناد دور متداخل بالفعل بنفس النطاق.");
        var entity = new UserRoleAssignmentEntity { RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = roleKey, ScopeType = scopeType, ScopeBuildingId = string.IsNullOrWhiteSpace(buildingId) ? null : buildingId, ScopeApartmentId = string.IsNullOrWhiteSpace(apartmentId) ? null : apartmentId, ValidFromUtc = from, ValidToUtc = validToUtc.HasValue ? DateTime.SpecifyKind(validToUtc.Value, DateTimeKind.Utc) : null, GrantedAtUtc = DateTime.UtcNow, GrantedByAccountId = actor, GrantReason = reason };
        db.UserRoleAssignments.Add(entity);
        await db.SaveChangesAsync(ct);
        await _audit.LogAsync("identity.role.granted", buildingId: buildingId, apartmentId: apartmentId, userId: actor.ToString(), userRole: "superadmin", metadata: new { entity.RoleAssignmentId, accountId, roleKey, scopeType, reason });
        return (true, "تم منح الدور بالنطاق المحدد.");
    }

    public async Task<(bool Success, string Message)> RevokeRoleAsync(Guid assignmentId, string reason, Guid actor, CancellationToken ct)
    {
        reason = (reason ?? "").Trim();
        if (reason.Length is < 3 or > 500) return (false, "سبب الإلغاء مطلوب (3 إلى 500 حرف).");
        await using var db = await _factory.CreateDbContextAsync(ct);
        var entity = await db.UserRoleAssignments.SingleOrDefaultAsync(x => x.RoleAssignmentId == assignmentId, ct);
        if (entity == null) return (false, "إسناد الدور غير موجود.");
        if (entity.RoleKey == "superadmin") return (false, "لا يمكن إلغاء SuperAdmin من هذه الشاشة.");
        if (entity.RevokedAtUtc != null) return (true, "الدور ملغى بالفعل.");
        entity.RevokedAtUtc = DateTime.UtcNow; entity.RevokedByAccountId = actor; entity.RevocationReason = reason;
        await db.SaveChangesAsync(ct);
        await _audit.LogAsync("identity.role.revoked", buildingId: entity.ScopeBuildingId, apartmentId: entity.ScopeApartmentId, userId: actor.ToString(), userRole: "superadmin", metadata: new { assignmentId, entity.RoleKey, reason });
        return (true, "تم إلغاء إسناد الدور مع الاحتفاظ بسجل المنح والإلغاء.");
    }

    public async Task<(bool Success, string Message)> CreatePendingAccountAsync(Guid personId, Guid actor, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var person = await db.Persons.SingleOrDefaultAsync(x => x.PersonId == personId && !x.IsArchived, ct);
        if (person == null) return (false, "الهوية غير موجودة أو مؤرشفة.");
        if (await db.UserAccounts.AnyAsync(x => x.PersonId == personId, ct)) return (false, "يوجد حساب بالفعل لهذه الهوية.");
        var now = DateTime.UtcNow;
        if (!await db.ApartmentOccupancies.AnyAsync(x => x.PersonId == personId && x.ValidFromUtc <= now && (x.ValidToUtc == null || x.ValidToUtc > now), ct))
            return (false, "لا يمكن إنشاء حساب ساكن دون إشغال نشط مرتبط بشقة.");
        var account = new UserAccountEntity { AccountId = Guid.NewGuid(), PersonId = personId, AccountStatus = 3, MustChangePin = true, SecurityStamp = Guid.NewGuid().ToString("N"), CreatedAtUtc = now };
        db.UserAccounts.Add(account);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        await _audit.LogAsync("identity.account.created.pending", userId: actor.ToString(), userRole: "superadmin", metadata: new { account.AccountId, personId, credentialIssued = false, accountStatus = account.AccountStatus });
        return (true, "تم إنشاء حساب انتظار دون PIN. استخدم مركز الحسابات للتحقق وإصدار PIN لاحقًا.");
    }

    private static string? Clean(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Trim();
        return result.Length <= max ? result : result[..max];
    }
}
