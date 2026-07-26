using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IEventStatusSyncService
{
    Task<EventStatusSyncResultDto> SynchronizeAsync(
        CancellationToken cancellationToken = default);
}
