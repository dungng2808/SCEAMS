namespace SCEAMS.MVC.Models.Api;

public sealed class EventFaqEventApiResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ClubName { get; init; } = string.Empty;
    public string VenueName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int Capacity { get; init; }
    public int RegisteredCount { get; init; }
    public int SlotsRemaining { get; init; }
}
