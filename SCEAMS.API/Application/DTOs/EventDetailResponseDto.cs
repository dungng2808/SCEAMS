using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.DTOs;

public sealed class EventDetailResponseDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public EventStatus Status { get; init; }
    public int ClubId { get; init; }
    public string ClubName { get; init; } = string.Empty;
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string VenueLocation { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public DateTime RegistrationDeadline { get; init; }
    public int Capacity { get; init; }
    public int RegisteredCount { get; init; }
    public int SlotsRemaining { get; init; }
    public int CreatedByUserId { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
    public string? CancellationReason { get; init; }
    public string? CurrentRegistrationStatus { get; init; }
    public int? CurrentRegistrationId { get; init; }
    public bool CanFeedback { get; init; }
    public FeedbackResponseDto? CurrentFeedback { get; init; }
    public string? NotificationCorrelationId { get; init; }
    public bool? NotificationDelivered { get; init; }
    public string? NotificationError { get; init; }
    public EventActionPermissionsDto Permissions { get; init; } = new();
}

public sealed class EventActionPermissionsDto
{
    public bool CanEdit { get; init; }
    public bool CanSubmit { get; init; }
    public bool CanApprove { get; init; }
    public bool CanReject { get; init; }
    public bool CanCancel { get; init; }
    public bool CanRegister { get; init; }
}
