using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>Creates and reviews resident phone correction requests without allowing self-service identity changes.</summary>
public sealed class PersonPhoneCorrectionService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IAuditLogger _audit;

    public PersonPhoneCorrectionService(IDbContextFactory<AppDbContext> factory, IAuditLogger audit)
    {
        _factory = factory;
        _audit = audit;
    }

    public async Task<bool> SubmitAsync(string previousPhone, string requestedPhone, string buildingId, string apartmentId)
    {
        var oldPhone = AuthHelpers.NormalizePhone(previousPhone);
        var newPhone = AuthHelpers.NormalizePhone(requestedPhone);
        if (string.IsNullOrWhiteSpace(oldPhone) || string.IsNullOrWhiteSpace(newPhone) ||
            oldPhone == newPhone || string.IsNullOrWhiteSpace(buildingId) || string.IsNullOrWhiteSpace(apartmentId))
            return false;

        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var people = await db.Persons.Where(p => p.PrimaryPhone != null).ToListAsync();
        var oldMatches = people.Where(p => AuthHelpers.NormalizePhone(p.PrimaryPhone) == oldPhone).ToList();
        if (oldMatches.Count != 1 || oldMatches[0].IsArchived) return false;
        if (people.Any(p => AuthHelpers.NormalizePhone(p.PrimaryPhone) == newPhone)) return false;

        var person = oldMatches[0];
        var now = DateTime.UtcNow;
        var hasOccupancy = await db.ApartmentOccupancies.AnyAsync(o =>
            o.PersonId == person.PersonId && o.BuildingId == buildingId && o.ApartmentId == apartmentId &&
            o.ValidFromUtc <= now && (o.ValidToUtc == null || o.ValidToUtc > now));
        if (!hasOccupancy) return false;

        if (await db.PersonPhoneChangeRequests.AnyAsync(r => r.PersonId == person.PersonId && r.Status == 0))
            return false;

        db.PersonPhoneChangeRequests.Add(new PersonPhoneChangeRequestEntity
        {
            RequestId = Guid.NewGuid(), PersonId = person.PersonId, BuildingId = buildingId,
            ApartmentId = apartmentId, PreviousPhone = oldPhone, RequestedPhone = newPhone,
            Status = 0, RequestedAtUtc = now
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<List<PhoneCorrectionQueueItem>> GetPendingAsync(string? buildingId, bool superAdmin)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = from request in db.PersonPhoneChangeRequests.AsNoTracking()
                    join person in db.Persons.AsNoTracking() on request.PersonId equals person.PersonId
                    where request.Status == 0 && (superAdmin || request.BuildingId == buildingId)
                    orderby request.RequestedAtUtc
                    select new PhoneCorrectionQueueItem
                    {
                        RequestId = request.RequestId, PersonName = person.DisplayName,
                        BuildingId = request.BuildingId, ApartmentId = request.ApartmentId,
                        PreviousPhone = request.PreviousPhone, RequestedPhone = request.RequestedPhone,
                        RequestedAtUtc = request.RequestedAtUtc
                    };
        return await query.Take(200).ToListAsync();
    }

    public async Task<bool> ReviewAsync(Guid requestId, string reviewerUserId, bool approve, string reason, bool superAdmin, string? buildingId)
    {
        if (string.IsNullOrWhiteSpace(reviewerUserId) || reviewerUserId.Length > 128 || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) return false;
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var request = await db.PersonPhoneChangeRequests.SingleOrDefaultAsync(r => r.RequestId == requestId && r.Status == 0);
        if (request == null || (!superAdmin && request.BuildingId != buildingId)) return false;

        var now = DateTime.UtcNow;
        if (approve)
        {
            var person = await db.Persons.SingleOrDefaultAsync(p => p.PersonId == request.PersonId && !p.IsArchived);
            if (person == null || AuthHelpers.NormalizePhone(person.PrimaryPhone) != request.PreviousPhone)
                return false;
            var collision = await db.Persons.Where(p => p.PersonId != request.PersonId && p.PrimaryPhone != null)
                .ToListAsync();
            if (collision.Any(p => AuthHelpers.NormalizePhone(p.PrimaryPhone) == request.RequestedPhone))
                return false;
            person.PrimaryPhone = request.RequestedPhone;
            person.UpdatedAtUtc = now;

            // A phone change is an identity change: rotate the account stamp so any
            // stamp-aware sessions/tokens can no longer rely on the old identity state.
            var linkedAccount = await db.UserAccounts.SingleOrDefaultAsync(a => a.PersonId == person.PersonId);
            if (linkedAccount != null)
                linkedAccount.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        request.Status = approve ? (byte)1 : (byte)2;
        request.ReviewedByUserId = reviewerUserId;
        request.ReviewedAtUtc = now;
        request.ReviewReason = reason.Trim();
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        await _audit.LogAsync(
            action: approve ? "resident.phone_change.approved" : "resident.phone_change.rejected",
            buildingId: request.BuildingId, apartmentId: request.ApartmentId,
            userId: reviewerUserId, userRole: superAdmin ? "superadmin" : "admin",
            metadata: new { requestId, personId = request.PersonId, oldPhone = request.PreviousPhone, newPhone = approve ? request.RequestedPhone : null },
            severity: "critical");
        return true;
    }
}

public sealed class PhoneCorrectionQueueItem
{
    public Guid RequestId { get; set; }
    public string PersonName { get; set; } = "";
    public string BuildingId { get; set; } = "";
    public string ApartmentId { get; set; } = "";
    public string PreviousPhone { get; set; } = "";
    public string RequestedPhone { get; set; } = "";
    public DateTime RequestedAtUtc { get; set; }
}