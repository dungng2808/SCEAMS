namespace SCEAMS.MVC.Models.Api;

public sealed class ChatHistoryItemApiResponse
{
    public int Id { get; init; }
    public string Question { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public IReadOnlyList<int> RelatedEventIds { get; init; } = [];
    public DateTime CreatedAt { get; init; }
}
