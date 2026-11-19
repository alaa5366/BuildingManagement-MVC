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
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-1",
                PersonId = eligibleId, OccupancyType = 1, IsPrimary = true,
                ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var report = await service.GetReportAsync();

        Assert.Equal(1, report.Eligible);
        Assert.Equal(1, report.ExistingAccounts);
        Assert.Equal(1, report.MissingPhone);
        Assert.Equal(2, report.DuplicatePhone);
        Assert.Equal(1, report.ArchivedPersons);
        Assert.Equal("Eligible", report.Rows.Single(x => x.PersonId == eligibleId).Status);
        Assert.Equal("Existing", report.Rows.Single(x => x.PersonId == existingId).Status);
    }

    [Fact]
    public async Task ProvisionBatchAsync_CreatesOnlyEligibleAccountAndRequiresInitialPinChange()
    {
        var factory = new TestContextFactory();
        var hasher = new BcryptPasswordHasher();
        var personId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(Person(personId, "Batch Resident", "01000000111", now));
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "resident", DisplayName = "Resident", IsActive = true
            });
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), BuildingId = "building-1", ApartmentId = "apt-11",
                PersonId = personId, OccupancyType = 1, IsPrimary = true,
                ValidFromUtc = now.AddDays(-1), RecordedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory, hasher);
        var result = await service.ProvisionBatchAsync(25, "superadmin-test");

        Assert.Equal(1, result.Requested);
        Assert.Equal(1, result.Created);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.RemainingEligible);

        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(x => x.PersonId == personId);
        Assert.Equal(1, account.AccountStatus);
        Assert.True(account.MustChangePin);
        var credential = await verify.UserCredentials.SingleAsync(x => x.AccountId == account.AccountId);
        Assert.Equal("BCrypt.Net-Next", credential.HashAlgorithm);
        Assert.True(hasher.Verify("1234", credential.CredentialHash));
        Assert.Single(await verify.UserRoleAssignments.Where(x =>
            x.AccountId == account.AccountId && x.RoleKey == "resident" &&
            x.ScopeBuildingId == "building-1" && x.ScopeApartmentId == "apt-11").ToListAsync());
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
        TestContextFactory factory,
        IPasswordHasher? hasher = null)
    {
        var audit = new Mock<IAuditLogger>();
        audit.Setup(x => x.LogAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return new ResidentAccountProvisioningService(
            factory,
            hasher ?? new BcryptPasswordHasher(),
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
