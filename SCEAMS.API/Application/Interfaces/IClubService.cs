using System.Security.Claims;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IClubService
{
    IQueryable<ClubResponseDto> GetClubsQuery(ClaimsPrincipal user);

    Task<Result<ClubDetailResponseDto>> GetClubByIdAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
