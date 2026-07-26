using System.Security.Claims;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class ClubService : IClubService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClubService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IQueryable<ClubResponseDto> GetClubsQuery(ClaimsPrincipal user)
    {
        var isAdminOrStaff = user.IsInRole(nameof(UserRole.Admin)) ||
                             user.IsInRole(nameof(UserRole.Staff));

        var query = _unitOfWork.Clubs.GetQueryable();

        if (!isAdminOrStaff)
        {
            query = query.Where(club => club.Status == ClubStatus.Approved);
        }

        return query.Select(club => new ClubResponseDto(
            club.Id,
            club.Name,
            club.Description,
            club.CategoryId,
            club.Category != null ? club.Category.Name : string.Empty,
            club.Status,
            club.CreatedByUserId,
            club.CreatedByUser != null ? club.CreatedByUser.FullName : string.Empty,
            club.Memberships.Count(m => m.Status == ClubMembershipStatus.Active),
            club.CreatedAt,
            club.ReviewedAt,
            club.RejectionReason,
            club.DissolvedAt));
    }
}
