using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.ViewModels;

public sealed class EventDetailViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ClubName { get; init; } = string.Empty;
    public string VenueName { get; init; } = string.Empty;
    public string VenueLocation { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public DateTime RegistrationDeadline { get; init; }
    public int Capacity { get; init; }
    public int RegisteredCount { get; init; }
    public int SlotsRemaining { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
    public string? CancellationReason { get; init; }
    public string? CurrentRegistrationStatus { get; init; }
    public int? CurrentRegistrationId { get; init; }
    public bool CanFeedback { get; init; }
    public EventFeedbackApiResponse? CurrentFeedback { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalFeedback { get; init; }
    public IReadOnlyList<EventFeedbackItemViewModel> Feedbacks { get; init; } = [];
    public EventPermissionsViewModel Permissions { get; init; } = new();
    public bool IsNotFound { get; init; }
    public string? ErrorMessage { get; init; }
    public string StatusClass => Status switch
    {
        "Approved" => "status-badge--success",
        "Ongoing" => "status-badge--info",
        "Completed" => "status-badge--neutral",
        "Cancelled" or "Rejected" => "status-badge--danger",
        _ => "status-badge--warning"
    };
}

public sealed class EventFeedbackItemViewModel
{
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class EventPermissionsViewModel
{
    public bool CanEdit { get; init; }
    public bool CanSubmit { get; init; }
    public bool CanApprove { get; init; }
    public bool CanReject { get; init; }
    public bool CanCancel { get; init; }
    public bool CanRegister { get; init; }
}
