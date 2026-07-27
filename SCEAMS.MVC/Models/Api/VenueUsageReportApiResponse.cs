namespace SCEAMS.MVC.Models.Api;

public sealed class VenueUsageReportApiResponse
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public IReadOnlyList<VenueUsageReportItemApiResponse> Items { get; init; } = [];
}

public sealed class VenueUsageReportItemApiResponse
{
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public int EventCount { get; init; }
    public decimal TotalHours { get; init; }
}
