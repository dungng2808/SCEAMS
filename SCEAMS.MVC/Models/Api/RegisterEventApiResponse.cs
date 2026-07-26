namespace SCEAMS.MVC.Models.Api;

public sealed class RegisterEventApiResponse
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public int RegisteredCount { get; init; }
    public int SlotsRemaining { get; init; }
}
