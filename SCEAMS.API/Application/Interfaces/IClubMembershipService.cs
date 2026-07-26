using System.Security.Claims;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IClubMembershipService
{
    Task<Result<ClubMembershipResponseDto>> RequestJoinClubAsync(
        int clubId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
