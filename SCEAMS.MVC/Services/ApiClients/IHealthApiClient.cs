using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IHealthApiClient
{
    Task<HealthApiResponse?> GetHealthAsync(
        CancellationToken cancellationToken = default);

    Task<DatabaseHealthApiResponse?> GetDatabaseHealthAsync(
        CancellationToken cancellationToken = default);
}
