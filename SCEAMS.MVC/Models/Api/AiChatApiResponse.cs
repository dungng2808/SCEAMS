namespace SCEAMS.MVC.Models.Api;

public sealed class AiChatApiResponse
{
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public IReadOnlyList<EventFaqEventApiResponse> RelatedEvents { get; init; } = [];
}
