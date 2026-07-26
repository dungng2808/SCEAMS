namespace SCEAMS.MVC.ViewModels;

public sealed class EventRegistrationsViewModel
{
    public int EventId { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<EventRegistrationItemViewModel> Items { get; init; } = [];
}

public sealed class EventRegistrationItemViewModel
{
    public int Id { get; init; }
    public string StudentCode { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
    public DateTime EventStartTime { get; init; }
    public DateTime EventEndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public bool IsAttended { get; init; }
    public DateTime? CheckInTime { get; init; }
    public int? CheckedInByUserId { get; init; }
    public string? CheckedInByUserName { get; init; }
}
