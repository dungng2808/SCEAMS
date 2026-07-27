namespace SCEAMS.MVC.Models.Api;

public sealed class VenueUsageReportApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public bool IsBadRequest { get; init; }
    public string? ErrorMessage { get; init; }
    public VenueUsageReportApiResponse? Report { get; init; }
}
