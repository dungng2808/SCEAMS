namespace SCEAMS.MVC.Models.Api;

public sealed class UpdateClubApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsForbidden { get; init; }
    public bool IsConflict { get; init; }
    public bool IsUnauthorized { get; init; }
    public string? ErrorMessage { get; init; }
    public ClubDetailApiResponse? Club { get; init; }
}
