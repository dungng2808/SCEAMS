using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IAuthApiClient
{
    Task<LoginApiResult> LoginAsync(
        LoginApiRequest request,
        CancellationToken cancellationToken = default);

    Task<RegisterStudentApiResult> RegisterStudentAsync(
        RegisterStudentApiRequest request,
        CancellationToken cancellationToken = default);

    Task<RefreshTokenApiResult> RefreshTokenAsync(
        RefreshTokenApiRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeTokenAsync(
        RefreshTokenApiRequest request,
        CancellationToken cancellationToken = default);
}
