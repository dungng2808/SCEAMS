using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class EventReminderService : IEventReminderService
{
    public const string NotificationType = "EventRegistrationDeadlineReminder";

    private readonly IEventRepository _eventRepository;
    private readonly INotificationReminderStore _reminderStore;
    private readonly INotificationClientService _notificationClientService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EventReminderService> _logger;

    public EventReminderService(
        IEventRepository eventRepository,
        INotificationReminderStore reminderStore,
        INotificationClientService notificationClientService,
        TimeProvider timeProvider,
        ILogger<EventReminderService> logger)
    {
        _eventRepository = eventRepository;
        _reminderStore = reminderStore;
        _notificationClientService = notificationClientService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<EventReminderResultDto>> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var events = await _eventRepository.GetEventsWithUpcomingDeadlineAsync(
            now,
            now.AddHours(24),
            cancellationToken);
        var sent = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var eventEntity in events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var claimed = await _reminderStore.TryClaimAsync(
                eventEntity.Id,
                NotificationType,
                cancellationToken);
            if (!claimed)
            {
                skipped++;
                continue;
            }

            var notification = await _notificationClientService
                .NotifyEventReminderAsync(
                    eventEntity.Id,
                    eventEntity.Title,
                    eventEntity.CreatedByUserId,
                    cancellationToken);
            if (notification.Success)
            {
                await _reminderStore.MarkSentAsync(
                    eventEntity.Id,
                    NotificationType,
                    now,
                    cancellationToken);
                sent++;
            }
            else
            {
                await _reminderStore.MarkFailedAsync(
                    eventEntity.Id,
                    NotificationType,
                    notification.ErrorMessage ?? "Notification service không phản hồi.",
                    cancellationToken);
                failed++;
                _logger.LogWarning(
                    "Reminder failed for Event {EventId}; correlation {CorrelationId}.",
                    eventEntity.Id,
                    notification.CorrelationId);
            }
        }

        return Result<EventReminderResultDto>.Ok(new EventReminderResultDto
        {
            Scanned = events.Count,
            Sent = sent,
            Skipped = skipped,
            Failed = failed
        });
    }
}
