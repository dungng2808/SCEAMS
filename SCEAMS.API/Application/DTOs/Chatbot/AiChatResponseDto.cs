namespace SCEAMS.Application.DTOs.Chatbot;

public sealed class AiChatResponseDto
{
    public string Question { get; init; } = string.Empty;
    public string Answer { get; init; } = string.Empty;
    public IReadOnlyList<EventFaqEventDto> RelatedEvents { get; init; } = [];
}
