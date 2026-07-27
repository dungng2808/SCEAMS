namespace SCEAMS.Domain.Entities;

public sealed class NotificationDelivery
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }

    public Event Event { get; set; } = null!;
}
