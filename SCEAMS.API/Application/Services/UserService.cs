using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;

namespace SCEAMS.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
