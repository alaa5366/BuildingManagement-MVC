using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetPollsServiceTests
{
    [Fact]
    public async Task VoteAsync_StoresAnonymousProtectedBallot_AndConsumesOnlyEligibility()
    {
        var options = CreateOptions();
        await SeedBuildingAsync(options, includeApartment: true);
        var service = CreateService(options);
        var poll = await service.CreateAsync("B1", "Approve the repair?", null,
            new[] { "Yes", "No" }, null, "admin");

        Assert.True(await service.VoteAsync("B1", poll.Id, "A1", poll.Options[0].Id));
        Assert.False(await service.DeleteAsync("B1", poll.Id));

        await using var verify = new AppDbContext(options);
        var ballot = await verify.PollBallots.SingleAsync(x => x.BuildingId == "B1" && x.PollId == poll.Id);
        var entitlement = await verify.PollEligibility.SingleAsync(x =>
            x.BuildingId == "B1" && x.PollId == poll.Id && x.SubjectReference == "A1");
        Assert.NotNull(entitlement.ConsumedAtUtc);
        Assert.DoesNotContain("A1", System.Text.Encoding.UTF8.GetString(ballot.ChoicePayload), StringComparison.Ordinal);
        Assert.DoesNotContain(poll.Options[0].Id, System.Text.Encoding.UTF8.GetString(ballot.ChoicePayload), StringComparison.Ordinal);
        Assert.Equal(32, ballot.BallotTokenHash.Length);
        Assert.Empty(await verify.PollVotes.Where(x => x.BuildingId == "B1" && x.PollId == poll.Id).ToListAsync());

        var residentView = await service.GetAsync("B1", poll.Id, subjectReference: "A1");
        Assert.NotNull(residentView);
        Assert.True(residentView!.HasVoted);
        Assert.Equal(1, residentView.TotalVotes);
        Assert.Equal(1, residentView.Options.Single(x => x.Id == poll.Options[0].Id).VoteCount);
        Assert.Empty(residentView.Votes);
    }

    [Fact]
    public async Task VoteAsync_RejectsSecondVoteWithoutCreatingAnotherBallot()
    {
        var options = CreateOptions();
        await SeedBuildingAsync(options, includeApartment: true);
        var service = CreateService(options);
        var poll = await service.CreateAsync("B1", "Choose?", null, new[] { "A", "B" }, null, "admin");

        Assert.True(await service.VoteAsync("B1", poll.Id, "A1", poll.Options[0].Id));
        Assert.False(await service.VoteAsync("B1", poll.Id, "A1", poll.Options[1].Id));

        await using var verify = new AppDbContext(options);
        Assert.Single(await verify.PollBallots.Where(x => x.PollId == poll.Id).ToListAsync());
        Assert.Single(await verify.PollEligibility.Where(x => x.PollId == poll.Id && x.ConsumedAtUtc != null).ToListAsync());
    }

    [Fact]
    public async Task VoteAsync_RejectsApartmentThatWasNotEligibleWhenPollWasCreated()
    {
        var options = CreateOptions();
        await SeedBuildingAsync(options, includeApartment: false);
        var service = CreateService(options);
        var poll = await service.CreateAsync("B1", "Choose?", null, new[] { "A", "B" }, null, "admin");
        await using (var seed = new AppDbContext(options))
        {
            seed.Apartments.Add(new ApartmentEntity { BuildingId = "B1", Id = "A2", FloorId = "F1", Number = 2 });
            await seed.SaveChangesAsync();
        }

        Assert.False(await service.VoteAsync("B1", poll.Id, "A2", poll.Options[0].Id));
    }

    [Fact]
    public async Task DeleteAsync_RemovesUnvotedPollEntitlementsButRetainsLifecycleEvidence()
    {
        var options = CreateOptions();
        await SeedBuildingAsync(options, includeApartment: true);
        var service = CreateService(options);
        var poll = await service.CreateAsync("B1", "Draft question?", null, new[] { "Yes", "No" }, null, "admin");

        Assert.True(await service.DeleteAsync("B1", poll.Id));

        await using var verify = new AppDbContext(options);
        Assert.False(await verify.Polls.AnyAsync(x => x.BuildingId == "B1" && x.Id == poll.Id));
        Assert.Empty(await verify.PollEligibility.Where(x => x.PollId == poll.Id).ToListAsync());
        Assert.Empty(await verify.PollVotingPolicies.Where(x => x.PollId == poll.Id).ToListAsync());
        Assert.Contains(await verify.PollLifecycleEvents.ToListAsync(), x => x.PollId == poll.Id && x.EventType == 4);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnboundedPollInputs()
    {
        var options = CreateOptions();
        await SeedBuildingAsync(options, includeApartment: false);
        var service = CreateService(options);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            "B1", new string('Q', 301), null, new[] { "Yes", "No" }, null, "admin"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            "B1", "Question?", null, Enumerable.Range(1, 21).Select(i => $"Option {i}").ToArray(), null, "admin"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            "B1", "Question?", null, new[] { new string('X', 201), "No" }, null, "admin"));
    }

    private static DbContextOptions<AppDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;

    private static TargetPollsService CreateService(DbContextOptions<AppDbContext> options) =>
        new(new TestContextFactory(options), DataProtectionProvider.Create("BuildingManagement.Tests"));

    private static async Task SeedBuildingAsync(DbContextOptions<AppDbContext> options, bool includeApartment)
    {
        await using var seed = new AppDbContext(options);
        seed.Buildings.Add(new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" });
        if (includeApartment)
            seed.Apartments.Add(new ApartmentEntity { BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1 });
        await seed.SaveChangesAsync();
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
