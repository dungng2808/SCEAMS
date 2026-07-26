using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class EventRegistrationListItemDto
{
    public int Id { get; init; }
    public string StudentCode { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public RegistrationStatus Status { get; init; }
    public DateTime RegisteredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public bool IsAttended { get; init; }
    public DateTime? CheckInTime { get; init; }
}
