namespace SCEAMS.Application.DTOs;

public sealed class VenueMaintenanceConflictDto
{
    public int EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
