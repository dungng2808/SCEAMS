using System.Security.Claims;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IEventService
{
    IQueryable<EventListResponseDto> GetEventsQuery(ClaimsPrincipal user);
}
