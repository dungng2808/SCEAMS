namespace SCEAMS.MVC.ViewModels;

public sealed class VenueScheduleViewModel
{
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public bool IsNotFound { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<VenueScheduleEventViewModel> Events { get; init; } = [];
}

public sealed class VenueScheduleEventViewModel
{
    public int EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
