using System.Net.Http.Json;
using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public sealed class HealthApiClient : IHealthApiClient
{
    private readonly HttpClient _httpClient;

    public HealthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HealthApiResponse?> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "api/health",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<HealthApiResponse>(
            cancellationToken: cancellationToken);
    }

    public async Task<DatabaseHealthApiResponse?> GetDatabaseHealthAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            "api/health/database",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<DatabaseHealthApiResponse>(
                cancellationToken: cancellationToken);
    }
}
