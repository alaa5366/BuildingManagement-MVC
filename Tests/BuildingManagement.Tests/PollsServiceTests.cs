using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Xunit;

namespace BuildingManagement.Tests;

public class PollsServiceTests
{
    private readonly PollsService _service = new();

    private static Building CreateBuilding()
    {
        var b = new Building { Name = "Test" };
        b.Apartments.Add(new Apartment { Id = "a1", Number = 1 });
        b.Apartments.Add(new Apartment { Id = "a2", Number = 2 });
        return b;
    }

    // ================================================================
    // CreatePoll
    // ================================================================

    [Fact]
    public void CreatePoll_ValidData_CreatesPoll()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Question?", "Desc", new List<string> { "A", "B" }, null, "admin");

        Assert.NotNull(poll);
        Assert.Equal("Question?", poll.Question);
        Assert.Equal(2, poll.Options.Count);
        Assert.Empty(poll.Votes);
        Assert.False(poll.Closed);
        Assert.Single(b.Polls);
    }

    [Fact]
    public void CreatePoll_EmptyOptions_FiltersThem()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "  ", "", "B" }, null, "admin");
        Assert.Equal(2, poll.Options.Count);
    }

    [Fact]
    public void CreatePoll_EmptyDeadline_NullDeadline()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, "", "admin");
        Assert.Null(poll.Deadline);
    }

    // ================================================================
    // Vote
    // ================================================================

    [Fact]
    public void Vote_ValidVote_ReturnsTrue()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, null, "admin");
        var optId = poll.Options[0].Id;

        var result = _service.Vote(b, poll.Id, "a1", optId);

        Assert.True(result);
        Assert.Equal(optId, poll.Votes["a1"]);
    }

    [Fact]
    public void Vote_DuplicateVote_ReturnsFalse()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, null, "admin");
        _service.Vote(b, poll.Id, "a1", poll.Options[0].Id);

        var result = _service.Vote(b, poll.Id, "a1", poll.Options[1].Id);

        Assert.False(result); // صوّت خلاص
    }

    [Fact]
    public void Vote_ClosedPoll_ReturnsFalse()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, null, "admin");
        _service.SetClosed(b, poll.Id, true);

        Assert.False(_service.Vote(b, poll.Id, "a1", poll.Options[0].Id));
    }

    [Fact]
    public void Vote_ExpiredPoll_ReturnsFalse()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, "2020-01-01T00:00:00Z", "admin");
        Assert.False(_service.Vote(b, poll.Id, "a1", poll.Options[0].Id));
    }

    [Fact]
    public void Vote_InvalidOption_ReturnsFalse()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, null, "admin");
        Assert.False(_service.Vote(b, poll.Id, "a1", "invalid-option"));
    }

    // ================================================================
    // IsPollActive
    // ================================================================

    [Fact]
    public void IsPollActive_OpenNoDeadline_ReturnsTrue()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, null, "admin");
        Assert.True(PollsService.IsPollActive(poll));
    }

    [Fact]
    public void IsPollActive_Closed_ReturnsFalse()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, null, "admin");
        poll.Closed = true;
        Assert.False(PollsService.IsPollActive(poll));
    }

    [Fact]
    public void IsPollActive_ExpiredDeadline_ReturnsFalse()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, "2020-01-01T00:00:00Z", "admin");
        Assert.False(PollsService.IsPollActive(poll));
    }

    // ================================================================
    // Delete & SetClosed
    // ================================================================

    [Fact]
    public void Delete_ExistingPoll_RemovesIt()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, null, "admin");

        var result = _service.Delete(b, poll.Id);

        Assert.True(result);
        Assert.Empty(b.Polls);
    }

    [Fact]
    public void SetClosed_UpdatesClosedFlag()
    {
        var b = CreateBuilding();
        var poll = _service.CreatePoll(b, "Q?", null, new List<string> { "A", "B" }, null, "admin");

        _service.SetClosed(b, poll.Id, true);

        Assert.True(poll.Closed);
    }
}