namespace SCEAMS.Application.DTOs.Chatbot;

public sealed class ChatHistoryPageDto
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public IReadOnlyList<ChatHistoryItemDto> Items { get; init; } = [];
}
