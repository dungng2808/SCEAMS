using System.Security.Claims;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IRegistrationService
{
    Task<Result<RegistrationResponseDto>> CreateAsync(
        CreateRegistrationRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<RegistrationResponseDto>> CancelAsync(
        int registrationId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<RegistrationHistoryItemDto>>> GetMyHistoryAsync(
        string? status,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<EventRegistrationListItemDto>>> GetEventRegistrationsAsync(
        int eventId,
        string? status,
        string? search,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<CheckInResponseDto>> CheckInAsync(
        int registrationId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
