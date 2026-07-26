namespace SCEAMS.MVC.Models.Api;

public sealed class VenueScheduleApiResponse
{
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime FromUtc { get; init; }
    public DateTime ToUtc { get; init; }
    public IReadOnlyList<VenueScheduleEventApiResponse> Events { get; init; } = [];
}
