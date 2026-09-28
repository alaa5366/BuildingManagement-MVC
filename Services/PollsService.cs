using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/features/polls.js
public class PollsService
{
    public List<Poll> GetPolls(Building building) =>
        building.Polls.OrderByDescending(p => p.CreatedAt).ToList();

    public static bool IsPollExpired(Poll poll)
    {
        if (string.IsNullOrWhiteSpace(poll.Deadline)) return false;
        return DateTime.TryParse(poll.Deadline, out var d) && d < DateTime.UtcNow;
    }

    public static bool IsPollActive(Poll poll) => !poll.Closed && !IsPollExpired(poll);

    public static int TotalVotes(Poll poll) => poll.Votes.Count;

    public static int OptionVotes(Poll poll, string optionId) => poll.Votes.Values.Count(v => v == optionId);

    public static string? MyVote(Poll poll, string aptId) => poll.Votes.TryGetValue(aptId, out var v) ? v : null;

    public Poll CreatePoll(Building building, string question, string? description, List<string> optionTexts, string? deadline, string createdBy)
    {
        var poll = new Poll
        {
            Id = "poll-" + Guid.NewGuid().ToString("N"),
            Question = question,
            Description = description ?? "",
            Options = optionTexts.Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => new PollOption { Id = "opt-" + Guid.NewGuid().ToString("N")[..8], Text = t.Trim() }).ToList(),
            Votes = new Dictionary<string, string>(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            Deadline = string.IsNullOrWhiteSpace(deadline) ? null : deadline,
            Closed = false
        };
        building.Polls.Add(poll);
        return poll;
    }

    // بيرجع false لو الشقة صوّتت قبل كده أو التصويت مقفول
    public bool Vote(Building building, string pollId, string aptId, string optionId)
    {
        var poll = building.Polls.FirstOrDefault(p => p.Id == pollId);
        if (poll == null || !IsPollActive(poll)) return false;
        if (poll.Votes.ContainsKey(aptId)) return false;
        if (!poll.Options.Any(o => o.Id == optionId)) return false;

        poll.Votes[aptId] = optionId;
        return true;
    }

    public bool SetClosed(Building building, string pollId, bool closed)
    {
        var poll = building.Polls.FirstOrDefault(p => p.Id == pollId);
        if (poll == null) return false;
        poll.Closed = closed;
        return true;
    }

    public bool Delete(Building building, string pollId)
    {
        var before = building.Polls.Count;
        building.Polls = building.Polls.Where(p => p.Id != pollId).ToList();
        return before != building.Polls.Count;
    }
}
