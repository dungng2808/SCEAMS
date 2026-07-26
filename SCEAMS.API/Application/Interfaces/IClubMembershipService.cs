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

    Task<Result<PagedResult<ClubMembershipResponseDto>>> GetPendingMembershipsAsync(
        int clubId,
        string? search,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<ClubMembershipResponseDto>> DecideMembershipAsync(
        int clubId,
        int userId,
        DecideClubMembershipRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
