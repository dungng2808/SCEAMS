using System.Security.Claims;
using SCEAMS.Application.DTOs;

namespace SCEAMS.Application.Interfaces;

public interface IClubService
{
    IQueryable<ClubResponseDto> GetClubsQuery(ClaimsPrincipal user);
}
