namespace SCEAMS.MVC.Models.Api;

public sealed class ChatHistoryPageApiResponse
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public IReadOnlyList<ChatHistoryItemApiResponse> Items { get; init; } = [];
}
