namespace SCEAMS.MVC.Models.Api;

public sealed class VenueApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public bool IsNotFound { get; init; }
    public VenueApiResponse? Venue { get; init; }
    public string? ErrorMessage { get; init; }
}
