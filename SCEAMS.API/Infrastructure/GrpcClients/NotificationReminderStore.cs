using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Infrastructure.Data;

namespace SCEAMS.Infrastructure.GrpcClients;

public sealed class NotificationReminderStore : INotificationReminderStore
{
    private readonly SceamsDbContext _dbContext;

    public NotificationReminderStore(SceamsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryClaimAsync(
        int eventId,
        string notificationType,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.NotificationDeliveries
            .SingleOrDefaultAsync(
                item => item.EventId == eventId &&
                        item.NotificationType == notificationType,
                cancellationToken);
        if (existing is not null)
        {
            if (existing.Status == "Sent" || existing.Status == "Pending")
            {
                return false;
            }

            existing.Status = "Pending";
            existing.ErrorMessage = null;
            existing.CorrelationId = Guid.NewGuid().ToString("N");
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        _dbContext.NotificationDeliveries.Add(new NotificationDelivery
        {
            EventId = eventId,
            NotificationType = notificationType,
            CorrelationId = Guid.NewGuid().ToString("N"),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task MarkSentAsync(
        int eventId,
        string notificationType,
        DateTime sentAt,
        CancellationToken cancellationToken = default)
    {
        var delivery = await FindAsync(eventId, notificationType, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        delivery.Status = "Sent";
        delivery.SentAt = sentAt;
        delivery.ErrorMessage = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        int eventId,
        string notificationType,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var delivery = await FindAsync(eventId, notificationType, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        delivery.Status = "Failed";
        delivery.ErrorMessage = errorMessage[..Math.Min(errorMessage.Length, 2000)];
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<NotificationDelivery?> FindAsync(
        int eventId,
        string notificationType,
        CancellationToken cancellationToken) =>
        _dbContext.NotificationDeliveries.SingleOrDefaultAsync(
            item => item.EventId == eventId &&
                    item.NotificationType == notificationType,
            cancellationToken);
}
