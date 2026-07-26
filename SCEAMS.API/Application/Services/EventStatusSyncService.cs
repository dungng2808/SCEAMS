using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class EventStatusSyncService : IEventStatusSyncService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EventStatusSyncService> _logger;

    public EventStatusSyncService(
        IUnitOfWork unitOfWork,
        ILogger<EventStatusSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EventStatusSyncResultDto> SynchronizeAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var events = await _unitOfWork.Events
            .GetQueryable()
            .Where(eventEntity =>
                eventEntity.Status == EventStatus.Approved ||
                eventEntity.Status == EventStatus.Ongoing)
            .ToListAsync(cancellationToken);

        var toOngoing = 0;
        var toCompleted = 0;
        foreach (var eventEntity in events)
        {
            var nextStatus = eventEntity.EndTime <= now
                ? EventStatus.Completed
                : eventEntity.StartTime <= now &&
                  eventEntity.Status == EventStatus.Approved
                    ? EventStatus.Ongoing
                    : eventEntity.Status;

            if (nextStatus == eventEntity.Status)
            {
                continue;
            }

            eventEntity.Status = nextStatus;
            eventEntity.UpdatedAt = now;
            _unitOfWork.Events.Update(eventEntity);
            if (nextStatus == EventStatus.Ongoing)
            {
                toOngoing++;
            }
            else if (nextStatus == EventStatus.Completed)
            {
                toCompleted++;
            }
        }

        if (toOngoing > 0 || toCompleted > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var result = new EventStatusSyncResultDto
        {
            CheckedAtUtc = now,
            ToOngoing = toOngoing,
            ToCompleted = toCompleted
        };
        _logger.LogInformation(
            "Event status sync checked {CheckedAtUtc}; transitioned {ToOngoing} to Ongoing and {ToCompleted} to Completed.",
            result.CheckedAtUtc,
            result.ToOngoing,
            result.ToCompleted);
        return result;
    }
}
