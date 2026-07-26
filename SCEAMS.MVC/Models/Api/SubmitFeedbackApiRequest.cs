namespace SCEAMS.MVC.Models.Api;

public sealed class SubmitFeedbackApiRequest
{
    public int Rating { get; init; }
    public string? Comment { get; init; }
}
