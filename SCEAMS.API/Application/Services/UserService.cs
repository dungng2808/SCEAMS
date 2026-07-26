using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly TimeProvider _timeProvider;

    public UserService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreatedUserResponseDto>>
        CreateUserAsync(
            CreateUserRequestDto request,
            CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Role))
        {
            return Result<CreatedUserResponseDto>.Fail(
                "Role is invalid.",
                StatusCodes.Status400BadRequest);
        }

        var fullName = NormalizeFullName(request.FullName);
        var email = request.Email.Trim().ToLowerInvariant();
        var studentCode = NormalizeStudentCode(
            request.StudentCode);

        if (fullName.Length < 2)
        {
            return Result<CreatedUserResponseDto>.Fail(
                "FullName must contain at least 2 characters.",
                StatusCodes.Status400BadRequest);
        }

        if (request.Role == UserRole.Student &&
            studentCode is null)
        {
            return Result<CreatedUserResponseDto>.Fail(
                "StudentCode is required for Student role.",
                StatusCodes.Status400BadRequest);
        }

        if (await _unitOfWork.Users.EmailExistsAsync(
                email,
                cancellationToken))
        {
            return Result<CreatedUserResponseDto>.Fail(
                "Email is already registered.",
                StatusCodes.Status409Conflict);
        }

        if (studentCode is not null &&
            await _unitOfWork.Users.StudentCodeExistsAsync(
                studentCode,
                cancellationToken))
        {
            return Result<CreatedUserResponseDto>.Fail(
                "StudentCode is already registered.",
                StatusCodes.Status409Conflict);
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            StudentCode = studentCode,
            PhoneNumber = NormalizeOptionalValue(
                request.PhoneNumber),
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedAt = _timeProvider
                .GetUtcNow()
                .UtcDateTime
        };

        user.PasswordHash = _passwordService.HashPassword(
            user,
            request.Password);

        await _unitOfWork.Users.AddAsync(
            user,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreatedUserResponseDto>.Created(
            new CreatedUserResponseDto(
                Id: user.Id,
                FullName: user.FullName,
                Email: user.Email,
                StudentCode: user.StudentCode,
                PhoneNumber: user.PhoneNumber,
                Role: user.Role.ToString(),
                IsActive: user.IsActive,
                CreatedAt: user.CreatedAt),
            "User account created successfully.");
    }

    public async Task<Result<UpdatedUserResponseDto>>
        UpdateUserProfileAsync(
            int userId,
            UpdateUserProfileRequestDto request,
            CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(
            userId,
            cancellationToken);

        if (user is null)
        {
            return Result<UpdatedUserResponseDto>.Fail(
                "User account does not exist.",
                StatusCodes.Status404NotFound);
        }

        var fullName = NormalizeFullName(request.FullName);
        var email = request.Email.Trim().ToLowerInvariant();
        var studentCode = NormalizeStudentCode(
            request.StudentCode);

        if (fullName.Length < 2)
        {
            return Result<UpdatedUserResponseDto>.Fail(
                "FullName must contain at least 2 characters.",
                StatusCodes.Status400BadRequest);
        }

        if (user.Role == UserRole.Student &&
            studentCode is null)
        {
            return Result<UpdatedUserResponseDto>.Fail(
                "StudentCode is required for Student role.",
                StatusCodes.Status400BadRequest);
        }

        if (await _unitOfWork.Users
            .EmailBelongsToOtherUserAsync(
                email,
                userId,
                cancellationToken))
        {
            return Result<UpdatedUserResponseDto>.Fail(
                "Email is already registered.",
                StatusCodes.Status409Conflict);
        }

        if (studentCode is not null &&
            await _unitOfWork.Users
                .StudentCodeBelongsToOtherUserAsync(
                    studentCode,
                    userId,
                    cancellationToken))
        {
            return Result<UpdatedUserResponseDto>.Fail(
                "StudentCode is already registered.",
                StatusCodes.Status409Conflict);
        }

        user.FullName = fullName;
        user.Email = email;
        user.StudentCode = studentCode;
        user.PhoneNumber = NormalizeOptionalValue(
            request.PhoneNumber);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UpdatedUserResponseDto>.Ok(
            new UpdatedUserResponseDto(
                Id: user.Id,
                FullName: user.FullName,
                Email: user.Email,
                StudentCode: user.StudentCode,
                PhoneNumber: user.PhoneNumber,
                Role: user.Role.ToString(),
                IsActive: user.IsActive,
                CreatedAt: user.CreatedAt),
            "User profile updated successfully.");
    }

    public async Task<Result<UserActiveStatusResponseDto>>
        UpdateUserActiveStatusAsync(
            int actingAdminId,
            int userId,
            UpdateUserActiveStatusRequestDto request,
            CancellationToken cancellationToken = default)
    {
        if (!request.IsActive.HasValue)
        {
            return Result<UserActiveStatusResponseDto>.Fail(
                "IsActive is required.",
                StatusCodes.Status400BadRequest);
        }

        var isActive = request.IsActive.Value;

        if (actingAdminId == userId &&
            !isActive)
        {
            return Result<UserActiveStatusResponseDto>.Fail(
                "Administrators cannot lock their own account.",
                StatusCodes.Status400BadRequest);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(
            userId,
            cancellationToken);

        if (user is null)
        {
            return Result<UserActiveStatusResponseDto>.Fail(
                "User account does not exist.",
                StatusCodes.Status404NotFound);
        }

        var shouldSave = false;

        if (user.IsActive != isActive)
        {
            user.IsActive = isActive;
            shouldSave = true;
        }

        if (!isActive &&
            (user.RefreshTokenHash is not null ||
             user.RefreshTokenExpiresAt is not null))
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            shouldSave = true;
        }

        if (shouldSave)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<UserActiveStatusResponseDto>.Ok(
            new UserActiveStatusResponseDto(
                Id: user.Id,
                FullName: user.FullName,
                Email: user.Email,
                Role: user.Role.ToString(),
                IsActive: user.IsActive),
            isActive
                ? "User account unlocked successfully."
                : "User account locked successfully.");
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

        var normalizedFullName = NormalizeFullName(
            request.FullName);

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
        User user)
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

    private static string NormalizeFullName(string fullName)
    {
        return string.Join(
            ' ',
            fullName.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries));
    }

    private static string? NormalizeStudentCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
