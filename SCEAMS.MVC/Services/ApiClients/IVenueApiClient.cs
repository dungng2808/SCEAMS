using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IVenueApiClient
{
    Task<DeleteVenueApiResult> DeleteVenueAsync(
        int venueId,
        CancellationToken cancellationToken = default);

    Task<VenueScheduleApiResult> GetScheduleAsync(
        int venueId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    Task<VenueApiResult> GetVenueAsync(
        int venueId,
        CancellationToken cancellationToken = default);

    Task<CreateVenueApiResult> CreateVenueAsync(
        CreateVenueApiRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateVenueApiResult> UpdateVenueAsync(
        int venueId,
        UpdateVenueApiRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateVenueApiResult> UpdateMaintenanceAsync(
        int venueId,
        UpdateVenueMaintenanceApiRequest request,
        CancellationToken cancellationToken = default);

    Task<VenueListApiResult> GetVenuesAsync(
        string? search,
        bool? maintenance,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
