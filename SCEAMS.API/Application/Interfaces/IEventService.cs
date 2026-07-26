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

    Task<Result<EventDetailResponseDto>> CreateEventAsync(
        CreateEventRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<EventDetailResponseDto>> UpdateEventAsync(
        int id,
        UpdateEventRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<EventDetailResponseDto>> SubmitEventAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
