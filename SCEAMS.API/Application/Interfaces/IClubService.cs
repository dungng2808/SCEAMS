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

    Task<Result<ClubDetailResponseDto>> CreateClubAsync(
        CreateClubRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<ClubDetailResponseDto>> ApproveClubAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<ClubDetailResponseDto>> RejectClubAsync(
        int id,
        RejectClubRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<ClubDetailResponseDto>> UpdateClubAsync(
        int id,
        UpdateClubRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<Result<ClubDetailResponseDto>> DissolveClubAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
