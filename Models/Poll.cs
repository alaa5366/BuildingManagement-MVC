using Google.Cloud.Firestore;

namespace BuildingManagementMvc.Models;

// نفس شكل عنصر building.polls[] في Firestore (features/polls.js)
[FirestoreData]
public class Poll
{
    [FirestoreProperty("id")] public string Id { get; set; } = "";
    [FirestoreProperty("question")] public string Question { get; set; } = "";
    [FirestoreProperty("description")] public string Description { get; set; } = "";
    [FirestoreProperty("options")] public List<PollOption> Options { get; set; } = new();
    // aptId -> optionId
    [FirestoreProperty("votes")] public Dictionary<string, string> Votes { get; set; } = new();
    [FirestoreProperty("createdBy")] public string CreatedBy { get; set; } = "";
    [FirestoreProperty("createdAt")] public string CreatedAt { get; set; } = "";
    [FirestoreProperty("deadline")] public string? Deadline { get; set; }
    [FirestoreProperty("closed")] public bool Closed { get; set; }
}

[FirestoreData]
public class PollOption
{
    [FirestoreProperty("id")] public string Id { get; set; } = "";
    [FirestoreProperty("text")] public string Text { get; set; } = "";
}
