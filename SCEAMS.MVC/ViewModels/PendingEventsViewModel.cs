namespace SCEAMS.MVC.ViewModels;

public sealed class PendingEventsViewModel
{
    public int? ClubId { get; init; }
    public int? VenueId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<EventListItemViewModel> Events { get; init; } = [];
}
