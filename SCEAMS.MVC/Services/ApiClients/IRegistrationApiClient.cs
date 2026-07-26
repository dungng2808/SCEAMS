using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IRegistrationApiClient
{
    Task<RegistrationHistoryApiResult> GetMyRegistrationsAsync(
        string? status,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
