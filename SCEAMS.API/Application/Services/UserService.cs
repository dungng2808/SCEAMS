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

        var response = new CurrentUserProfileResponseDto(
            Id: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            StudentCode: user.StudentCode,
            PhoneNumber: user.PhoneNumber,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt);

        return Result<CurrentUserProfileResponseDto>.Ok(response);
    }
}
