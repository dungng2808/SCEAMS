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
        CancellationToken cancellationToken = default,
        string? notificationCorrelationId = null,
        bool? notificationDelivered = null,
        string? notificationError = null);

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

    Task<Result<PagedResult<EventListResponseDto>>> GetPendingApprovalEventsAsync(
        int? clubId,
        int? venueId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<EventDetailResponseDto>> ApproveEventAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<EventDetailResponseDto>> RejectEventAsync(
        int id,
        RejectEventRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<EventDetailResponseDto>> CancelEventAsync(
        int id,
        CancelEventRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
