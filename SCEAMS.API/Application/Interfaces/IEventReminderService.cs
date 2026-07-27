using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface INotificationReminderStore
{
    Task<bool> TryClaimAsync(
        int eventId,
        string notificationType,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(
        int eventId,
        string notificationType,
        DateTime sentAt,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        int eventId,
        string notificationType,
        string errorMessage,
        CancellationToken cancellationToken = default);
}

public interface IEventReminderService
{
    Task<Result<EventReminderResultDto>> RunAsync(
        CancellationToken cancellationToken = default);
}
