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
}
