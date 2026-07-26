namespace SCEAMS.Application.DTOs;

public sealed class FeedbackSummaryResponseDto
{
    public int EventId { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalFeedback { get; init; }
    public IReadOnlyList<FeedbackListItemDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
