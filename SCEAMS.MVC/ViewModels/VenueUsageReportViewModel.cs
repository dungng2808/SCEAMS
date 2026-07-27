namespace SCEAMS.MVC.ViewModels;

public sealed class VenueUsageReportViewModel
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<VenueUsageReportItemViewModel> Items { get; init; } = [];
}

public sealed class VenueUsageReportItemViewModel
{
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public int EventCount { get; init; }
    public decimal TotalHours { get; init; }
}
