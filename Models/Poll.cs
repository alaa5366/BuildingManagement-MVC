namespace BuildingManagementMvc.Models;

/// <summary>
/// A poll with options and votes.
/// </summary>
public class Poll
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the question.
    /// </summary>
    public string Question { get; set; } = "";
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string Description { get; set; } = "";
    /// <summary>
    /// Gets or sets the options.
    /// </summary>
    public List<PollOption> Options { get; set; } = new();
    /// <summary>
    /// Gets or sets the votes.
    /// </summary>
    public Dictionary<string, string> Votes { get; set; } = new();
    /// <summary>
    /// Gets or sets the created by.
    /// </summary>
    public string CreatedBy { get; set; } = "";
    /// <summary>
    /// Gets or sets the created date and time.
    /// </summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the deadline.
    /// </summary>
    public string? Deadline { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is closed.
    /// </summary>
    public bool Closed { get; set; }
    /// <summary>Gets or sets the aggregate number of valid anonymous ballots.</summary>
    public int TotalVotes { get; set; }
    /// <summary>Gets or sets whether the current resident has consumed their voting entitlement.</summary>
    public bool HasVoted { get; set; }
}

/// <summary>
/// One option of a poll.
/// </summary>
public class PollOption
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string Text { get; set; } = "";
    /// <summary>Gets or sets the aggregate number of ballots for this option.</summary>
    public int VoteCount { get; set; }
}