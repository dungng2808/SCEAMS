using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IUserService
{
    Task<Result<CurrentUserProfileResponseDto>> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Result<CurrentUserProfileResponseDto>>
        UpdateCurrentUserAsync(
            int userId,
            UpdateCurrentUserProfileRequestDto request,
            CancellationToken cancellationToken = default);

    Task<Result> ChangeCurrentUserPasswordAsync(
        int userId,
        ChangeCurrentUserPasswordRequestDto request,
        CancellationToken cancellationToken = default);
}
