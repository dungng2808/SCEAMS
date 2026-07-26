namespace SCEAMS.MVC.Models.Api;

public sealed class UpdateCurrentUserProfileApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsNotFound { get; init; }
    public CurrentUserProfileApiResponse? Profile { get; init; }
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } =
        new Dictionary<string, string[]>();
    public string? ErrorMessage { get; init; }
}
