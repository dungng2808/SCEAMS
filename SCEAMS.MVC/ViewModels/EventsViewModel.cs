namespace SCEAMS.MVC.ViewModels;

public sealed class EventsViewModel
{
    public string? Search { get; init; }
    public int? ClubId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? Status { get; init; }
    public bool? HasSlots { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<EventListItemViewModel> Events { get; init; } = [];
}

public sealed class EventListItemViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string ClubName { get; init; } = string.Empty;
    public string VenueName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int Capacity { get; init; }
    public int RegisteredCount { get; init; }
    public int SlotsRemaining { get; init; }
    public string StatusClass => Status switch
    {
        "Approved" => "status-badge--success",
        "Ongoing" => "status-badge--info",
        "Completed" => "status-badge--neutral",
        "Cancelled" or "Rejected" => "status-badge--danger",
        _ => "status-badge--warning"
    };
}
