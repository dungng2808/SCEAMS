using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    public async Task<Result<PagedUsersResponseDto>> GetUsersAsync(
        UserListQueryDto query,
        CancellationToken cancellationToken = default)
    {
        if (query.Role.HasValue &&
            !Enum.IsDefined(query.Role.Value))
        {
            return Result<PagedUsersResponseDto>.Fail(
                "Role filter is invalid.",
                StatusCodes.Status400BadRequest);
        }

        var search = NormalizeOptionalValue(query.Search);
        var page = await _unitOfWork.Users.GetPagedAsync(
            search,
            query.Role,
            query.IsActive,
            query.Page,
            query.PageSize,
            cancellationToken);
        var totalPages = page.TotalItems / query.PageSize +
            (page.TotalItems % query.PageSize == 0 ? 0 : 1);
        var response = new PagedUsersResponseDto(
            Items: page.Items,
            Page: query.Page,
            PageSize: query.PageSize,
            TotalItems: page.TotalItems,
            TotalPages: totalPages,
            HasPreviousPage: query.Page > 1,
            HasNextPage: query.Page < totalPages);

        return Result<PagedUsersResponseDto>.Ok(response);
    }

    public async Task<Result<CurrentUserProfileResponseDto>>
        GetCurrentUserAsync(
            int userId,
            CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(
            userId,
            cancellationToken);

        if (user is null)
        {
            return Result<CurrentUserProfileResponseDto>.Fail(
                "User account no longer exists.",
                StatusCodes.Status404NotFound);
        }

        return Result<CurrentUserProfileResponseDto>.Ok(
            MapProfile(user));
    }

    public async Task<Result<CurrentUserProfileResponseDto>>
        UpdateCurrentUserAsync(
            int userId,
            UpdateCurrentUserProfileRequestDto request,
            CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(
            userId,
            cancellationToken);

        if (user is null)
        {
            return Result<CurrentUserProfileResponseDto>.Fail(
                "User account no longer exists.",
                StatusCodes.Status404NotFound);
        }

        var normalizedFullName = string.Join(
            ' ',
            request.FullName.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries));

        if (normalizedFullName.Length < 2)
        {
            return Result<CurrentUserProfileResponseDto>.Fail(
                "FullName must contain at least 2 characters.",
                StatusCodes.Status400BadRequest);
        }

        user.FullName = normalizedFullName;
        user.PhoneNumber = NormalizeOptionalValue(
            request.PhoneNumber);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CurrentUserProfileResponseDto>.Ok(
            MapProfile(user),
            "Profile updated successfully.");
    }

    public async Task<Result> ChangeCurrentUserPasswordAsync(
        int userId,
        ChangeCurrentUserPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(
            userId,
            cancellationToken);

        if (user is null)
        {
            return Result.Fail(
                "User account no longer exists.",
                StatusCodes.Status404NotFound);
        }

        if (!_passwordService.VerifyPassword(
                user,
                user.PasswordHash,
                request.CurrentPassword))
        {
            return Result.Fail(
                "Current password is incorrect.",
                StatusCodes.Status400BadRequest);
        }

        user.PasswordHash = _passwordService.HashPassword(
            user,
            request.NewPassword);
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.NoContent(
            "Password changed successfully.");
    }

    private static CurrentUserProfileResponseDto MapProfile(
        SCEAMS.Domain.Entities.User user)
    {
        return new CurrentUserProfileResponseDto(
            Id: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            StudentCode: user.StudentCode,
            PhoneNumber: user.PhoneNumber,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt);
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
