namespace SCEAMS.Application.DTOs.Reports;

public sealed class VenueUsageReportDto
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public IReadOnlyList<VenueUsageItemDto> Items { get; init; } = [];
}

public sealed class VenueUsageItemDto
{
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public int EventCount { get; init; }
    public decimal TotalHours { get; init; }
}
