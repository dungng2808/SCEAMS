namespace SCEAMS.Application.DTOs;

public sealed class VenueResponseDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public bool IsUnderMaintenance { get; init; }
}
