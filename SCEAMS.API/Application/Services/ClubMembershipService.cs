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

public sealed class ClubMembershipService : IClubMembershipService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClubMembershipService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ClubMembershipResponseDto>> RequestJoinClubAsync(
        int clubId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var studentUserId) || studentUserId <= 0)
        {
            return Result<ClubMembershipResponseDto>.Fail(
                "Không xác định được danh tính sinh viên từ token xác thực.",
                StatusCodes.Status401Unauthorized);
        }

        var student = await _unitOfWork.Users.GetByIdAsync(studentUserId, cancellationToken);
        if (student == null)
        {
            return Result<ClubMembershipResponseDto>.Fail(
                "Tài khoản sinh viên không tồn tại trong hệ thống.",
                StatusCodes.Status401Unauthorized);
        }

        var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken);
        if (club == null)
        {
            return Result<ClubMembershipResponseDto>.Fail(
                $"Câu lạc bộ với ID {clubId} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        if (club.Status != ClubStatus.Approved)
        {
            return Result<ClubMembershipResponseDto>.Fail(
                $"Không thể gửi yêu cầu gia nhập câu lạc bộ không ở trạng thái Hoạt động. Trạng thái hiện tại: {club.Status}.",
                StatusCodes.Status409Conflict);
        }

        var existingMemberships = await _unitOfWork.ClubMemberships.FindAsync(
            m => m.ClubId == clubId && m.StudentId == studentUserId,
            cancellationToken);

        var existing = existingMemberships.FirstOrDefault();

        if (existing != null)
        {
            if (existing.Status == ClubMembershipStatus.Pending)
            {
                return Result<ClubMembershipResponseDto>.Fail(
                    "Bạn đã gửi đơn xin gia nhập câu lạc bộ này và đang chờ duyệt.",
                    StatusCodes.Status409Conflict);
            }

            if (existing.Status == ClubMembershipStatus.Active)
            {
                return Result<ClubMembershipResponseDto>.Fail(
                    "Bạn đã là thành viên chính thức của câu lạc bộ này.",
                    StatusCodes.Status409Conflict);
            }

            // Re-apply if previous status was Rejected or Removed
            existing.Status = ClubMembershipStatus.Pending;
            existing.JoinDate = DateTime.UtcNow;
            existing.DecidedByUserId = null;
            existing.DecisionAt = null;
            existing.RemovalReason = null;
            existing.RoleInClub = "Member";

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updatedDto = new ClubMembershipResponseDto
            {
                Id = existing.Id,
                StudentId = studentUserId,
                StudentName = student.FullName,
                StudentEmail = student.Email,
                ClubId = club.Id,
                ClubName = club.Name,
                RoleInClub = existing.RoleInClub,
                JoinDate = existing.JoinDate,
                Status = existing.Status,
                DecidedByUserId = null,
                DecisionAt = null,
                RemovalReason = null
            };

            return Result<ClubMembershipResponseDto>.Created(updatedDto);
        }

        var newMembership = new ClubMembership
        {
            ClubId = clubId,
            StudentId = studentUserId,
            RoleInClub = "Member",
            JoinDate = DateTime.UtcNow,
            Status = ClubMembershipStatus.Pending
        };

        await _unitOfWork.ClubMemberships.AddAsync(newMembership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ClubMembershipResponseDto
        {
            Id = newMembership.Id,
            StudentId = studentUserId,
            StudentName = student.FullName,
            StudentEmail = student.Email,
            ClubId = club.Id,
            ClubName = club.Name,
            RoleInClub = newMembership.RoleInClub,
            JoinDate = newMembership.JoinDate,
            Status = newMembership.Status,
            DecidedByUserId = null,
            DecisionAt = null,
            RemovalReason = null
        };

        return Result<ClubMembershipResponseDto>.Created(dto);
    }

    public async Task<Result<PagedResult<ClubMembershipResponseDto>>> GetPendingMembershipsAsync(
        int clubId,
        string? search,
        int page,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var currentUserId) || currentUserId <= 0)
        {
            return Result<PagedResult<ClubMembershipResponseDto>>.Fail(
                "Không xác định được danh tính người dùng từ token xác thực.",
                StatusCodes.Status401Unauthorized);
        }

        var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken);
        if (club == null)
        {
            return Result<PagedResult<ClubMembershipResponseDto>>.Fail(
                $"Câu lạc bộ với ID {clubId} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var isAdminOrStaff = user.IsInRole(nameof(UserRole.Admin)) ||
                             user.IsInRole(nameof(UserRole.Staff));
        var isOwner = club.CreatedByUserId == currentUserId;

        if (!isAdminOrStaff && !isOwner)
        {
            return Result<PagedResult<ClubMembershipResponseDto>>.Fail(
                "Bạn không có quyền xem danh sách đơn xin gia nhập của câu lạc bộ này.",
                StatusCodes.Status403Forbidden);
        }

        var queryable = _unitOfWork.Clubs.GetQueryable()
            .Where(c => c.Id == clubId)
            .SelectMany(c => c.Memberships)
            .Where(m => m.Status == ClubMembershipStatus.Pending);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTrimmed = search.Trim().ToLower();
            queryable = queryable.Where(m =>
                m.Student.FullName.ToLower().Contains(searchTrimmed) ||
                m.Student.Email.ToLower().Contains(searchTrimmed));
        }

        var totalItems = await queryable.CountAsync(cancellationToken);

        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
        var skip = (normalizedPage - 1) * normalizedPageSize;

        var items = await queryable
            .OrderByDescending(m => m.JoinDate)
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(m => new ClubMembershipResponseDto
            {
                Id = m.Id,
                StudentId = m.StudentId,
                StudentName = m.Student.FullName,
                StudentEmail = m.Student.Email,
                ClubId = m.ClubId,
                ClubName = m.Club.Name,
                RoleInClub = m.RoleInClub,
                JoinDate = m.JoinDate,
                Status = m.Status,
                DecidedByUserId = m.DecidedByUserId,
                DecisionAt = m.DecisionAt,
                RemovalReason = m.RemovalReason
            })
            .ToListAsync(cancellationToken);

        var pagedResult = new PagedResult<ClubMembershipResponseDto>(items, totalItems);

        return Result<PagedResult<ClubMembershipResponseDto>>.Ok(pagedResult);
    }

    public async Task<Result<ClubMembershipResponseDto>> DecideMembershipAsync(
        int clubId,
        int userId,
        DecideClubMembershipRequestDto request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdClaim, out var currentUserId) || currentUserId <= 0)
        {
            return Result<ClubMembershipResponseDto>.Fail(
                "Không xác định được danh tính người dùng từ token xác thực.",
                StatusCodes.Status401Unauthorized);
        }

        var club = await _unitOfWork.Clubs.GetByIdAsync(clubId, cancellationToken);
        if (club == null)
        {
            return Result<ClubMembershipResponseDto>.Fail(
                $"Câu lạc bộ với ID {clubId} không tồn tại.",
                StatusCodes.Status404NotFound);
        }

        var isAdminOrStaff = user.IsInRole(nameof(UserRole.Admin)) ||
                             user.IsInRole(nameof(UserRole.Staff));
        var isOwner = club.CreatedByUserId == currentUserId;

        if (!isAdminOrStaff && !isOwner)
        {
            return Result<ClubMembershipResponseDto>.Fail(
                "Bạn không có quyền duyệt hoặc từ chối đơn xin gia nhập câu lạc bộ này.",
                StatusCodes.Status403Forbidden);
        }

        var memberships = await _unitOfWork.ClubMemberships.FindAsync(
            m => m.StudentId == userId && m.ClubId == clubId,
            cancellationToken);

        var membership = memberships.FirstOrDefault();
        if (membership == null)
        {
            return Result<ClubMembershipResponseDto>.Fail(
                $"Người dùng #{userId} không có đơn xin gia nhập trong câu lạc bộ này.",
                StatusCodes.Status404NotFound);
        }

        if (membership.Status != ClubMembershipStatus.Pending)
        {
            return Result<ClubMembershipResponseDto>.Fail(
                $"Chỉ đơn xin gia nhập ở trạng thái Chờ duyệt (Pending) mới có thể xử lý. Trạng thái hiện tại: {membership.Status}.",
                StatusCodes.Status409Conflict);
        }

        var student = await _unitOfWork.Users.GetByIdAsync(membership.StudentId, cancellationToken);

        if (request.Approve)
        {
            membership.Status = ClubMembershipStatus.Active;
            membership.DecidedByUserId = currentUserId;
            membership.DecisionAt = DateTime.UtcNow;
            membership.RemovalReason = null;
        }
        else
        {
            membership.Status = ClubMembershipStatus.Rejected;
            membership.DecidedByUserId = currentUserId;
            membership.DecisionAt = DateTime.UtcNow;
            membership.RemovalReason = request.RejectionReason?.Trim();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ClubMembershipResponseDto
        {
            Id = membership.Id,
            StudentId = membership.StudentId,
            StudentName = student?.FullName ?? string.Empty,
            StudentEmail = student?.Email ?? string.Empty,
            ClubId = club.Id,
            ClubName = club.Name,
            RoleInClub = membership.RoleInClub,
            JoinDate = membership.JoinDate,
            Status = membership.Status,
            DecidedByUserId = membership.DecidedByUserId,
            DecisionAt = membership.DecisionAt,
            RemovalReason = membership.RemovalReason
        };

        return Result<ClubMembershipResponseDto>.Ok(dto);
    }
}
