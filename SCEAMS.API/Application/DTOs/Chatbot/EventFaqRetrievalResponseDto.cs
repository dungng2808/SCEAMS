namespace SCEAMS.Application.DTOs.Chatbot;

public sealed class EventFaqRetrievalResponseDto
{
    public string Question { get; init; } = string.Empty;
    public IReadOnlyList<EventFaqEventDto> RelatedEvents { get; init; } = [];
}
