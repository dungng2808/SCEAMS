using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Application.Services;
using SCEAMS.Domain.Enums;
using SCEAMS.NotificationService;

namespace SCEAMS.Infrastructure.GrpcClients;

public sealed class NotificationClientService : INotificationClientService, IDisposable
{
    private readonly Notification.NotificationClient _client;
    private readonly GrpcChannel _channel;
    private readonly NotificationGrpcOptions _options;
    private readonly INotificationLogStore _logStore;
    private readonly ILogger<NotificationClientService> _logger;

    public NotificationClientService(
        IOptions<NotificationGrpcOptions> options,
        INotificationLogStore logStore,
        ILogger<NotificationClientService> logger)
    {
        _options = options.Value;
        _channel = GrpcChannel.ForAddress(_options.Address);
        _client = new Notification.NotificationClient(_channel);
        _logStore = logStore;
        _logger = logger;
    }

    public async Task<NotificationDispatchResult> NotifyEventStatusChangedAsync(
        int eventId,
        string eventTitle,
        EventStatus status,
        int recipientUserId,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(
            eventId,
            eventTitle,
            status.ToString(),
            $"Event{status}Notification",
            recipientUserId,
            cancellationToken);
    }

    public async Task<NotificationDispatchResult> NotifyEventReminderAsync(
        int eventId,
        string eventTitle,
        int recipientUserId,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(
            eventId,
            eventTitle,
            EventStatus.Approved.ToString(),
            EventReminderService.NotificationType,
            recipientUserId,
            cancellationToken);
    }

    private async Task<NotificationDispatchResult> SendAsync(
        int eventId,
        string eventTitle,
        string eventStatus,
        string notificationType,
        int recipientUserId,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var request = new EventNotificationRequest
        {
            EventId = eventId,
            EventTitle = eventTitle,
            EventStatus = eventStatus,
            RecipientUserId = recipientUserId,
            NotificationType = notificationType,
            CorrelationId = correlationId
        };
        var attempts = Math.Clamp(_options.MaxRetries, 0, 3) + 1;
        string? lastError = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(
                    Math.Clamp(_options.TimeoutSeconds, 1, 30));
                var response = await _client.PublishEventNotificationAsync(
                    request,
                    deadline: deadline,
                    cancellationToken: cancellationToken);
                var result = new NotificationDispatchResult(
                    response.Accepted,
                    response.CorrelationId,
                    response.Accepted ? null : response.Message);
                _logStore.Add(new NotificationLogEntryDto
                {
                    EventId = eventId,
                    EventTitle = eventTitle,
                    EventStatus = eventStatus,
                    NotificationType = request.NotificationType,
                    RecipientUserId = recipientUserId,
                    CorrelationId = correlationId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsSuccess = result.Success,
                    ErrorMessage = result.ErrorMessage
                });
                return result;
            }
            catch (RpcException exception) when (
                exception.StatusCode is StatusCode.Unavailable or
                StatusCode.DeadlineExceeded)
            {
                lastError = exception.Status.Detail;
                _logger.LogWarning(
                    exception,
                    "Notification gRPC attempt {Attempt}/{Attempts} failed for Event {EventId}; correlation {CorrelationId}.",
                    attempt,
                    attempts,
                    eventId,
                    correlationId);
                if (attempt < attempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
                }
            }
            catch (Exception exception) when (
                exception is HttpRequestException or TaskCanceledException)
            {
                lastError = exception.Message;
                _logger.LogWarning(
                    exception,
                    "Notification gRPC request failed for Event {EventId}; correlation {CorrelationId}.",
                    eventId,
                    correlationId);
                break;
            }
        }

        var failure = new NotificationDispatchResult(false, correlationId, lastError);
        _logStore.Add(new NotificationLogEntryDto
        {
            EventId = eventId,
            EventTitle = eventTitle,
            EventStatus = eventStatus,
            NotificationType = request.NotificationType,
            RecipientUserId = recipientUserId,
            CorrelationId = correlationId,
            CreatedAt = DateTimeOffset.UtcNow,
            IsSuccess = false,
            ErrorMessage = lastError ?? "Notification service không phản hồi."
        });
        return failure;
    }

    public void Dispose() => _channel.Dispose();
}
