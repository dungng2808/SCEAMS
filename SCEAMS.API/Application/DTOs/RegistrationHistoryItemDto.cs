using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class RegistrationHistoryItemDto
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public EventStatus EventStatus { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public RegistrationStatus RegistrationStatus { get; init; }
    public DateTime RegisteredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public bool IsAttended { get; init; }
    public DateTime? CheckInTime { get; init; }
}
