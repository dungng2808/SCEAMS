using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IUserApiClient
{
    Task<CurrentUserProfileApiResult> GetCurrentUserAsync(
        CancellationToken cancellationToken = default);
}
