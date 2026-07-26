using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IAuthService
{
    Task<Result<RegisteredStudentResponseDto>> RegisterStudentAsync(
        RegisterStudentRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<RefreshTokenResponseDto>> RefreshAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default);
}
