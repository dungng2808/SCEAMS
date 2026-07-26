namespace SCEAMS.Application.DTOs;

public sealed class VenueScheduleResponseDto
{
    public int VenueId { get; init; }
    public string VenueName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime FromUtc { get; init; }
    public DateTime ToUtc { get; init; }
    public IReadOnlyList<VenueScheduleEventDto> Events { get; init; } = [];
}
