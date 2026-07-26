namespace SCEAMS.MVC.Models.Api;

public sealed class UserListApiResult
{
    public bool IsSuccess { get; init; }
    public bool IsUnauthorized { get; init; }
    public bool IsForbidden { get; init; }
    public PagedUsersApiResponse? Users { get; init; }
    public string? ErrorMessage { get; init; }
}
