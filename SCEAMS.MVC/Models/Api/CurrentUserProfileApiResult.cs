namespace SCEAMS.MVC.Models.Api;

public sealed class CurrentUserProfileApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsNotFound { get; init; }
    public CurrentUserProfileApiResponse? Profile { get; init; }
    public string? ErrorMessage { get; init; }
}
