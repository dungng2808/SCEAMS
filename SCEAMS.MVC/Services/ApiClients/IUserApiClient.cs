using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IUserApiClient
{
    Task<UserListApiResult> GetUsersAsync(
        UserListApiQuery query,
        CancellationToken cancellationToken = default);

    Task<CurrentUserProfileApiResult> GetCurrentUserAsync(
        CancellationToken cancellationToken = default);

    Task<UpdateCurrentUserProfileApiResult>
        UpdateCurrentUserAsync(
            UpdateCurrentUserProfileApiRequest request,
            CancellationToken cancellationToken = default);

    Task<ChangeCurrentUserPasswordApiResult>
        ChangeCurrentUserPasswordAsync(
            ChangeCurrentUserPasswordApiRequest request,
            CancellationToken cancellationToken = default);
}
