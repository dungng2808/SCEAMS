namespace SCEAMS.MVC.Models.Api;

public sealed class VenueScheduleApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsValidationError { get; init; }
    public VenueScheduleApiResponse? Schedule { get; init; }
    public string? ErrorMessage { get; init; }
}
