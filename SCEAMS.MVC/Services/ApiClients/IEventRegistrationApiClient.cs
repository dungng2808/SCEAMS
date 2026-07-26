using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IEventRegistrationApiClient
{
    Task<EventRegistrationListApiResult> GetForEventAsync(
        int eventId,
        string? status,
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<CheckInApiResult> CheckInAsync(
        int registrationId,
        CancellationToken cancellationToken = default);
}
