namespace SCEAMS.Application.DTOs.Chatbot;

public sealed record AiPromptContext(
    string Question,
    IReadOnlyList<AiEventContextDto> Events);
