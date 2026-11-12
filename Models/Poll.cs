namespace BuildingManagementMvc.Models;

public class Poll
{
    public string Id { get; set; } = "";
    public string Question { get; set; } = "";
    public string Description { get; set; } = "";
    public List<PollOption> Options { get; set; } = new();
    public Dictionary<string, string> Votes { get; set; } = new();
    public string CreatedBy { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string? Deadline { get; set; }
    public bool Closed { get; set; }
}

public class PollOption
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
}