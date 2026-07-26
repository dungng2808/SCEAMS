using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IUserService
{
    Task<Result<CurrentUserProfileResponseDto>> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
