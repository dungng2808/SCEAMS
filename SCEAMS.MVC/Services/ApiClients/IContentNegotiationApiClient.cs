using SCEAMS.MVC.Models;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IContentNegotiationApiClient
{
    Task<ContentNegotiationApiResult> GetEventsAsync(
        string acceptMediaType,
        int top,
        CancellationToken cancellationToken = default);
}
