using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetPollsServiceTests
{
    [Fact]
    public async Task DeleteAsync_RefusesToDeletePollAfterFirstVote_AndPreservesEvidence()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" });
            seed.Apartments.Add(new ApartmentEntity
            {
                BuildingId = "B1", Id = "A1", FloorId = "F1", Number = 1
            });
            await seed.SaveChangesAsync();
        }

        var service = new TargetPollsService(new TestContextFactory(options));
        var poll = await service.CreateAsync("B1", "Approve the repair?", null,
            new[] { "Yes", "No" }, null, "admin");
        Assert.True(await service.VoteAsync("B1", poll.Id, "A1", poll.Options[0].Id));

        Assert.False(await service.DeleteAsync("B1", poll.Id));

        await using var verify = new AppDbContext(options);
        Assert.True(await verify.Polls.AnyAsync(item => item.BuildingId == "B1" && item.Id == poll.Id));
        Assert.Single(await verify.PollVotes.Where(item => item.BuildingId == "B1" && item.PollId == poll.Id).ToListAsync());
        Assert.Equal(2, await verify.PollOptions.CountAsync(item => item.BuildingId == "B1" && item.PollId == poll.Id));
    }

    [Fact]
    public async Task DeleteAsync_AllowsDeletingPollThatHasNeverReceivedVotes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" });
            await seed.SaveChangesAsync();
        }

        var service = new TargetPollsService(new TestContextFactory(options));
        var poll = await service.CreateAsync("B1", "Draft question?", null,
            new[] { "Yes", "No" }, null, "admin");

        Assert.True(await service.DeleteAsync("B1", poll.Id));

        await using var verify = new AppDbContext(options);
        Assert.False(await verify.Polls.AnyAsync(item => item.BuildingId == "B1" && item.Id == poll.Id));
    }

    [Fact]
    public async Task CreateAsync_RejectsUnboundedPollInputs()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using (var seed = new AppDbContext(options))
        {
            seed.Buildings.Add(new BuildingEntity { Id = "B1", BuildingNumber = "1", Name = "One" });
            await seed.SaveChangesAsync();
        }

        var service = new TargetPollsService(new TestContextFactory(options));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            "B1", new string('Q', 301), null, new[] { "Yes", "No" }, null, "admin"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            "B1", "Question?", null, Enumerable.Range(1, 21).Select(i => $"Option {i}").ToArray(), null, "admin"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            "B1", "Question?", null, new[] { new string('X', 201), "No" }, null, "admin"));
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
