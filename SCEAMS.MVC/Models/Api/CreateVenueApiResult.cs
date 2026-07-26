namespace SCEAMS.MVC.Models.Api;

public sealed class CreateVenueApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public bool IsConflict { get; init; }
    public bool IsValidationError { get; init; }
    public string? ErrorMessage { get; init; }
    public VenueApiResponse? Venue { get; init; }
}
