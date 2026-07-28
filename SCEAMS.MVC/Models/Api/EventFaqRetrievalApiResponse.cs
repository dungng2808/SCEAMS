namespace SCEAMS.MVC.Models.Api;

public sealed class EventFaqRetrievalApiResponse
{
    public string Question { get; init; } = string.Empty;
    public IReadOnlyList<EventFaqEventApiResponse> RelatedEvents { get; init; } = [];
}
