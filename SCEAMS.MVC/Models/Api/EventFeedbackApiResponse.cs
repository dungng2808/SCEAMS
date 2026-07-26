namespace SCEAMS.MVC.Models.Api;

public sealed class EventFeedbackApiResponse
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
