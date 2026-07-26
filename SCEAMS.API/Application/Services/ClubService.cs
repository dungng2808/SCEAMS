using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Common;
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

        return query.Select(club => new ClubResponseDto
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            CategoryId = club.CategoryId,
            CategoryName = club.Category != null ? club.Category.Name : string.Empty,
            Status = club.Status,
            CreatedByUserId = club.CreatedByUserId,
            CreatedByUserName = club.CreatedByUser != null ? club.CreatedByUser.FullName : string.Empty,
            ActiveMemberCount = club.Memberships.Count(m => m.Status == ClubMembershipStatus.Active),
            CreatedAt = club.CreatedAt,
            ReviewedAt = club.ReviewedAt,
            RejectionReason = club.RejectionReason,
            DissolvedAt = club.DissolvedAt
        });
    }

    public async Task<Result<ClubDetailResponseDto>> GetClubByIdAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var club = await _unitOfWork.Clubs.GetByIdWithDetailsAsync(id, cancellationToken);
        if (club == null)
        {
            return Result<ClubDetailResponseDto>.Fail(
                $"Câu lạc bộ với ID {id} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var isAdminOrStaff = user.IsInRole(nameof(UserRole.Admin)) ||
                             user.IsInRole(nameof(UserRole.Staff));

        if (!isAdminOrStaff && club.Status != ClubStatus.Approved)
        {
            return Result<ClubDetailResponseDto>.Fail(
                $"Câu lạc bộ với ID {id} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var activeMemberCount = await _unitOfWork.Clubs.GetQueryable()
            .Where(c => c.Id == id)
            .SelectMany(c => c.Memberships)
            .CountAsync(m => m.Status == ClubMembershipStatus.Active, cancellationToken);

        var dto = new ClubDetailResponseDto
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            CategoryId = club.CategoryId,
            CategoryName = club.Category?.Name ?? string.Empty,
            Status = club.Status,
            CreatedByUserId = club.CreatedByUserId,
            CreatedByUserName = club.CreatedByUser?.FullName ?? string.Empty,
            ActiveMemberCount = activeMemberCount,
            CreatedAt = club.CreatedAt,
            ReviewedAt = club.ReviewedAt,
            RejectionReason = club.RejectionReason,
            DissolvedAt = club.DissolvedAt
        };

        return Result<ClubDetailResponseDto>.Ok(dto);
    }
}
