namespace SCEAMS.MVC.Models.Api;

public sealed class EventRegistrationApiResponse
{
    public int Id { get; init; }
    public string StudentCode { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public bool IsAttended { get; init; }
    public DateTime? CheckInTime { get; init; }
}
