namespace SCEAMS.MVC.Models.Api;

public sealed class FeedbackSummaryApiResponse
{
    public int EventId { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalFeedback { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyList<FeedbackListItemApiResponse> Items { get; init; } = [];
}

public sealed class FeedbackListItemApiResponse
{
    public int Id { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
