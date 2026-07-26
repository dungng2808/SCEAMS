using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
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

    public async Task<Result<ClubDetailResponseDto>> CreateClubAsync(
        CreateClubRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var currentUserId) || currentUserId <= 0)
        {
            return Result<ClubDetailResponseDto>.Fail(
                "Không xác định được danh tính người dùng từ token xác thực.",
                StatusCodes.Status401Unauthorized);
        }

        var category = await _unitOfWork.ClubCategories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result<ClubDetailResponseDto>.Fail(
                $"Danh mục với ID {request.CategoryId} không tồn tại.",
                StatusCodes.Status400BadRequest);
        }

        var clubNameTrimmed = request.Name.Trim();
        var existingClubs = _unitOfWork.Clubs.GetQueryable();
        var nameExists = await existingClubs.AnyAsync(
            c => c.Name.ToLower() == clubNameTrimmed.ToLower(),
            cancellationToken);

        if (nameExists)
        {
            return Result<ClubDetailResponseDto>.Fail(
                $"Câu lạc bộ với tên '{clubNameTrimmed}' đã tồn tại.",
                StatusCodes.Status409Conflict);
        }

        var creator = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken);

        var club = new Club
        {
            Name = clubNameTrimmed,
            Description = request.Description?.Trim(),
            CategoryId = request.CategoryId,
            Status = ClubStatus.PendingApproval,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Clubs.AddAsync(club, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ClubDetailResponseDto
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            CategoryId = club.CategoryId,
            CategoryName = category.Name,
            Status = club.Status,
            CreatedByUserId = club.CreatedByUserId,
            CreatedByUserName = creator?.FullName ?? string.Empty,
            ActiveMemberCount = 0,
            CreatedAt = club.CreatedAt,
            ReviewedAt = null,
            RejectionReason = null,
            DissolvedAt = null
        };

        return Result<ClubDetailResponseDto>.Created(dto);
    }
}
