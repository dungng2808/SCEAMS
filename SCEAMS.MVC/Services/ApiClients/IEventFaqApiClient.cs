using SCEAMS.MVC.Models.Api;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IEventFaqApiClient
{
    Task<EventFaqRetrievalApiResult> RetrieveEventsAsync(
        EventFaqRetrievalApiRequest request,
        CancellationToken cancellationToken = default);

    Task<AiChatApiResult> AskAsync(
        EventFaqRetrievalApiRequest request,
        CancellationToken cancellationToken = default);
}
