namespace SCEAMS.MVC.Models.Api;

public sealed class VenueApiResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public bool IsUnderMaintenance { get; init; }
}
