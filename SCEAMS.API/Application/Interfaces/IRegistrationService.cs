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
}
