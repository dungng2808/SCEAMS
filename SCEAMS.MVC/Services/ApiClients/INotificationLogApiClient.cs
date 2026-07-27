using SCEAMS.MVC.Models;

namespace SCEAMS.MVC.Services.ApiClients;

public interface INotificationLogApiClient
{
    Task<NotificationLogApiResult> GetLogsAsync(
        int? eventId,
        string? notificationType,
        bool? success,
        CancellationToken cancellationToken = default);
}
