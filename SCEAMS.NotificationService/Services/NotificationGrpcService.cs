using Grpc.Core;

namespace SCEAMS.NotificationService.Services;

public sealed class NotificationGrpcService : Notification.NotificationBase
{
    private readonly NotificationLogStore _logStore;
    private readonly ILogger<NotificationGrpcService> _logger;

    public NotificationGrpcService(
        NotificationLogStore logStore,
        ILogger<NotificationGrpcService> logger)
    {
        _logStore = logStore;
        _logger = logger;
    }

    public override Task<NotificationAck> PublishEventNotification(
        EventNotificationRequest request,
        ServerCallContext context)
    {
        var entry = new NotificationLogEntry(
            request.EventId,
            request.EventTitle,
            request.EventStatus,
            request.NotificationType,
            request.RecipientUserId,
            request.CorrelationId,
            DateTimeOffset.UtcNow);
        var accepted = _logStore.TryAdd(entry);
        _logger.LogInformation(
            "Notification {Action} for Event {EventId}; correlation {CorrelationId}.",
            accepted ? "accepted" : "deduplicated",
            request.EventId,
            request.CorrelationId);

        return Task.FromResult(new NotificationAck
        {
            Accepted = accepted,
            CorrelationId = request.CorrelationId,
            Message = accepted ? "Notification accepted." : "Notification already accepted."
        });
    }
}
