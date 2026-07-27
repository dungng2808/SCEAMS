namespace SCEAMS.Application.DTOs;

public sealed class NotificationLogEntryDto
{
    public int EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public string EventStatus { get; init; } = string.Empty;
    public string NotificationType { get; init; } = string.Empty;
    public int RecipientUserId { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
}
