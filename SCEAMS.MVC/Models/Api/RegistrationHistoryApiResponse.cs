namespace SCEAMS.MVC.Models.Api;

public sealed class RegistrationHistoryApiResponse
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string EventStatus { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string RegistrationStatus { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public bool IsAttended { get; init; }
    public DateTime? CheckInTime { get; init; }
}
