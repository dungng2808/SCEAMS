using SCEAMS.MVC.Models;

namespace SCEAMS.MVC.Services.ApiClients;

public interface IEventReminderApiClient
{
    Task<EventReminderRunApiResult> RunAsync(
        CancellationToken cancellationToken = default);
}
