using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class ResidentAccountProvisioningServiceTests
{
    [Fact]
    public async Task GetReportAsync_ClassifiesMissingDuplicateExistingAndEligiblePeople()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var eligibleId = Guid.NewGuid();
        var existingId = Guid.NewGuid();
        var duplicateA = Guid.NewGuid();
        var duplicateB = Guid.NewGuid();
        var noPhoneId = Guid.NewGuid();

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.Persons.AddRange(
                Person(eligibleId, "Eligible", "+201000000101", now),
                Person(existingId, "Existing", "+201000000102", now),
                Person(duplicateA, "Duplicate A", "+201000000103", now),
                Person(duplicateB, "Duplicate B", "01000000103", now),
                Person(noPhoneId, "No phone", null, now));
            db.Persons.Add(Person(Guid.NewGuid(), "Archived duplicate", "+201000000103", now, archived: true));
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = Guid.NewGuid(), PersonId = existingId, AccountStatus = 1,
                SecurityStamp = "existing-stamp", CreatedAtUtc = now
            });
            db.ApartmentOccupancies.AddRange(
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-1",
                    PersonId = eligibleId, OccupancyType = 1, IsPrimary = true,
                    ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                },
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-2",
                    PersonId = duplicateA, OccupancyType = 1, IsPrimary = true,
                    ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                },
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-3",
                    PersonId = duplicateB, OccupancyType = 1, IsPrimary = true,
                    ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                },
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-4",
                    PersonId = noPhoneId, OccupancyType = 1, IsPrimary = true,
                    ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var report = await service.GetReportAsync();

        Assert.Equal(1, report.Eligible);
        Assert.Equal(4, report.Provisionable);
        Assert.Equal(1, report.ExistingAccounts);
        Assert.Equal(1, report.MissingPhone);
        Assert.Equal(2, report.DuplicatePhone);
        Assert.Equal(1, report.ArchivedPersons);
        Assert.Equal("Eligible", report.Rows.Single(x => x.PersonId == eligibleId).Status);
        Assert.Equal("DuplicatePhone", report.Rows.Single(x => x.PersonId == duplicateA).Status);
        Assert.Equal("MissingPhone", report.Rows.Single(x => x.PersonId == noPhoneId).Status);
        Assert.Equal("Existing", report.Rows.Single(x => x.PersonId == existingId).Status);

        var batch = await service.ProvisionBatchAsync(10, "superadmin-test");
        Assert.Equal(4, batch.Requested);
        Assert.Equal(4, batch.Created);
        Assert.Equal(0, batch.Skipped);
        Assert.Equal(0, batch.RemainingProvisionable);

        await using var verify = await factory.CreateDbContextAsync();
        var pendingAccounts = await verify.UserAccounts
            .Where(x => x.PersonId != existingId)
            .ToListAsync();
        Assert.Equal(4, pendingAccounts.Count);
        Assert.All(pendingAccounts, account =>
        {
            Assert.Equal((byte)0, account.AccountStatus);
            Assert.True(account.MustChangePin);
        });
        Assert.Empty(await verify.UserCredentials
            .Where(x => pendingAccounts.Select(account => account.AccountId).Contains(x.AccountId))
            .ToListAsync());
    }

    [Fact]
    public async Task ProvisionBatchAsync_CreatesOnePendingAccountAndPreservesEveryApartmentScope()
    {
        var factory = new TestContextFactory();
        var personId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(Person(personId, "Batch Resident", "01000000111", now));
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.ApartmentOccupancies.AddRange(
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-11",
                    PersonId = personId, OccupancyType = 1, IsPrimary = true,
                    ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                },
                new ApartmentOccupancyEntity
                {
                    OccupancyId = Guid.NewGuid(), BuildingId = "building-2", ApartmentId = "apt-12",
                    PersonId = personId, OccupancyType = 1, IsPrimary = false,
                    ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
                });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var result = await service.ProvisionBatchAsync(25, "superadmin-test");

        Assert.Equal(1, result.Requested);
        Assert.Equal(1, result.Created);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.RemainingProvisionable);

        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.PersonId == personId);
        Assert.Equal(0, account.AccountStatus);
        Assert.True(account.MustChangePin);
        Assert.Empty(await verify.UserCredentials.Where(x => x.AccountId == account.AccountId).ToListAsync());
        var assignments = await verify.UserRoleAssignments
            .Where(x => x.AccountId == account.AccountId && x.RoleKey == "resident")
            .ToListAsync();
        Assert.Equal(2, assignments.Count);
        Assert.Contains(assignments, x => x.ScopeBuildingId == "building-1" && x.ScopeApartmentId == "apt-11");
        Assert.Contains(assignments, x => x.ScopeBuildingId == "building-2" && x.ScopeApartmentId == "apt-12");
    }

    private static PersonEntity Person(Guid id, string name, string? phone, DateTime now, bool archived = false) =>
        new()
        {
            PersonId = id,
            DisplayName = name,
            PrimaryPhone = phone,
            IsArchived = archived,
            ArchivedAtUtc = archived ? now : null,
            CreatedAtUtc = now
        };

    private static ResidentAccountProvisioningService CreateService(
        TestContextFactory factory)
    {
        var audit = new Mock<IAuditLogger>();
        audit.Setup(x => x.LogAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return new ResidentAccountProvisioningService(
            factory,
            audit.Object,
            NullLogger<ResidentAccountProvisioningService>.Instance);
    }

    private sealed class TestContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(_options));
    }
}
