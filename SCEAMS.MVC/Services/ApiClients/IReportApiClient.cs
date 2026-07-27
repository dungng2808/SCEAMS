using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IReportApiClient
{
    Task<EventSummaryReportApiResult> GetEventSummaryAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);
}
