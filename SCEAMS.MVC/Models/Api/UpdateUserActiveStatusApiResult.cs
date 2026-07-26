namespace SCEAMS.MVC.Models.Api;

public sealed class UpdateUserActiveStatusApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsSelfLock { get; init; }
    public UserActiveStatusApiResponse? User { get; init; }
    public string? ErrorMessage { get; init; }
}
