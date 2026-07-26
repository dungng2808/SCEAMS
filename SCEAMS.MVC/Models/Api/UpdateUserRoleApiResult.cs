namespace SCEAMS.MVC.Models.Api;

public sealed class UpdateUserRoleApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public bool IsNotFound { get; init; }
    public bool IsLastActiveAdmin { get; init; }
    public UserRoleApiResponse? User { get; init; }
    public string? ErrorMessage { get; init; }
}
