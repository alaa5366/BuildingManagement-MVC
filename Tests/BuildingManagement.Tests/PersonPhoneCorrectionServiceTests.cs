using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class PersonPhoneCorrectionServiceTests
{
    [Fact]
    public async Task SubmitAsync_RequiresUniqueCurrentPhoneAndActiveOccupancy()
    {
        var harness = new Harness();
        await harness.SeedPersonAsync("01000000099", "building-1", "apt-1");

        var accepted = await harness.Service.SubmitAsync(
            "01000000099", "01100000099", "building-1", "apt-1");
        var rejectedWrongApartment = await harness.Service.SubmitAsync(
            "01000000099", "01200000099", "building-1", "apt-2");

        Assert.True(accepted);
        Assert.False(rejectedWrongApartment);
        await using var db = await harness.Factory.CreateDbContextAsync();
        var request = await db.PersonPhoneChangeRequests.SingleAsync();
        Assert.Equal("+20100000099", request.PreviousPhone);
        Assert.Equal("+20110000099", request.RequestedPhone);
        Assert.Equal((byte)0, request.Status);
    }

    [Fact]
    public async Task SubmitAsync_RejectsPhoneAlreadyAssignedToAnotherPerson()
    {
        var harness = new Harness();
        await harness.SeedPersonAsync("01000000099", "building-1", "apt-1");
        await harness.SeedPersonAsync("01100000099", "building-1", "apt-2");

        var accepted = await harness.Service.SubmitAsync(
            "01000000099", "01100000099", "building-1", "apt-1");

        Assert.False(accepted);
        await using var db = await harness.Factory.CreateDbContextAsync();
        Assert.Empty(db.PersonPhoneChangeRequests);
    }

    [Fact]
    public async Task ReviewAsync_ApprovalUpdatesPhoneRotatesSecurityStampAndRecordsReviewer()
    {
        var harness = new Harness();
        var personId = await harness.SeedPersonAsync("01000000099", "building-1", "apt-1", withAccount: true);
        Assert.True(await harness.Service.SubmitAsync(
            "01000000099", "01100000099", "building-1", "apt-1"));

        await using var db = await harness.Factory.CreateDbContextAsync();
        var requestId = await db.PersonPhoneChangeRequests.Select(x => x.RequestId).SingleAsync();
        var oldStamp = await db.UserAccounts.Select(x => x.SecurityStamp).SingleAsync();

        var reviewed = await harness.Service.ReviewAsync(
            requestId, "legacy-admin-uid-123", true, "تم التحقق من بطاقة الساكن", false, "building-1");

        Assert.True(reviewed);
        await using var verify = await harness.Factory.CreateDbContextAsync();
        var person = await verify.Persons.SingleAsync(x => x.PersonId == personId);
        var account = await verify.UserAccounts.SingleAsync();
        var request = await verify.PersonPhoneChangeRequests.SingleAsync();
        Assert.Equal("+20110000099", person.PrimaryPhone);
        Assert.NotEqual(oldStamp, account.SecurityStamp);
        Assert.Equal("legacy-admin-uid-123", request.ReviewedByUserId);
        Assert.Equal((byte)1, request.Status);
        harness.Audit.Verify(x => x.LogAsync(
            "resident.phone_change.approved", "building-1", "apt-1", "legacy-admin-uid-123",
            "admin", It.IsAny<object>(), "critical"), Times.Once);
    }

    [Fact]
    public async Task ReviewAsync_AdminCannotReviewAnotherBuildingRequest()
    {
        var harness = new Harness();
        await harness.SeedPersonAsync("01000000099", "building-1", "apt-1");
        Assert.True(await harness.Service.SubmitAsync(
            "01000000099", "01100000099", "building-1", "apt-1"));
        await using var db = await harness.Factory.CreateDbContextAsync();
        var requestId = await db.PersonPhoneChangeRequests.Select(x => x.RequestId).SingleAsync();

        var reviewed = await harness.Service.ReviewAsync(
            requestId, "admin-uid", false, "طلب غير تابع للعمارة", false, "building-2");

        Assert.False(reviewed);
        await using var verify = await harness.Factory.CreateDbContextAsync();
        Assert.Equal((byte)0, (await verify.PersonPhoneChangeRequests.SingleAsync()).Status);
    }

    private sealed class Harness
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public Mock<IAuditLogger> Audit { get; } = new();
        public IDbContextFactory<AppDbContext> Factory { get; }
        public PersonPhoneCorrectionService Service { get; }

        public Harness()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            var factory = new Mock<IDbContextFactory<AppDbContext>>();
            factory.Setup(item => item.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new AppDbContext(_options));
            Factory = factory.Object;
            Audit.Setup(item => item.LogAsync(
                    It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<object?>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            Service = new PersonPhoneCorrectionService(Factory, Audit.Object);
        }

        public async Task<Guid> SeedPersonAsync(
            string phone, string buildingId, string apartmentId, bool withAccount = false)
        {
            var personId = Guid.NewGuid();
            await using var db = new AppDbContext(_options);
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Test Resident",
                PrimaryPhone = phone, CreatedAtUtc = DateTime.UtcNow
            });
            db.ApartmentOccupancies.Add(new ApartmentOccupancyEntity
            {
                OccupancyId = Guid.NewGuid(), PersonId = personId,
                BuildingId = buildingId, ApartmentId = apartmentId,
                OccupancyType = 1, IsPrimary = true,
                ValidFromUtc = DateTime.UtcNow.AddDays(-1), RecordedAtUtc = DateTime.UtcNow
            });
            if (withAccount)
                db.UserAccounts.Add(new UserAccountEntity
                {
                    AccountId = Guid.NewGuid(), PersonId = personId, AccountStatus = 1,
                    SecurityStamp = "before-phone-change", CreatedAtUtc = DateTime.UtcNow
                });
            await db.SaveChangesAsync();
            return personId;
        }
    }
}