using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IVenueApiClient
{
    Task<CreateVenueApiResult> CreateVenueAsync(
        CreateVenueApiRequest request,
        CancellationToken cancellationToken = default);

    Task<VenueListApiResult> GetVenuesAsync(
        string? search,
        bool? maintenance,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
