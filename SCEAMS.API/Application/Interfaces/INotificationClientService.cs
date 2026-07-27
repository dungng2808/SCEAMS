using SCEAMS.Application.DTOs;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Interfaces;

public sealed record NotificationDispatchResult(
    bool Success,
    string CorrelationId,
    string? ErrorMessage = null);

public interface INotificationClientService
{
    Task<NotificationDispatchResult> NotifyEventStatusChangedAsync(
        int eventId,
        string eventTitle,
        EventStatus status,
        int recipientUserId,
        CancellationToken cancellationToken = default);

    Task<NotificationDispatchResult> NotifyEventReminderAsync(
        int eventId,
        string eventTitle,
        int recipientUserId,
        CancellationToken cancellationToken = default);
}

public interface INotificationLogStore
{
    void Add(NotificationLogEntryDto entry);

    IReadOnlyList<NotificationLogEntryDto> GetRecent(int limit = 100);
}
