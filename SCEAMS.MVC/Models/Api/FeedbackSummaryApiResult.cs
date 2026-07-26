namespace SCEAMS.MVC.Models.Api;

public sealed class FeedbackSummaryApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public string? ErrorMessage { get; init; }
    public FeedbackSummaryApiResponse? Summary { get; init; }
}
