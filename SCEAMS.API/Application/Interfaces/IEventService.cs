using System.Security.Claims;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IEventService
{
    IQueryable<EventListResponseDto> GetEventsQuery(ClaimsPrincipal user);

    Task<Result<EventDetailResponseDto>> GetEventByIdAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
