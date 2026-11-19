using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class AppDbContextImmutabilityTests
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    [Fact]
    public void SaveChanges_RejectsAuditEventUpdates()
    {
        using var db = CreateContext();
        var audit = new AuditEventEntity { AuditEventId = Guid.NewGuid(), EventType = "test" };
        db.Attach(audit);
        db.Entry(audit).Property(x => x.EventType).IsModified = true;

        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
    }

    [Fact]
    public void SaveChanges_RejectsLedgerEntryUpdates()
    {
        using var db = CreateContext();
        var entry = new LedgerEntryEntity { LedgerEntryId = Guid.NewGuid() };
        db.Attach(entry);
        db.Entry(entry).Property(x => x.Description).IsModified = true;

        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsPostedLedgerTransactionUpdates()
    {
        using var db = CreateContext();
        var transaction = new LedgerTransactionEntity
        {
            LedgerTransactionId = Guid.NewGuid(),
            Status = LedgerTransactionStatus.Posted
        };
        db.Attach(transaction);
        db.Entry(transaction).Property(x => x.Reason).IsModified = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
