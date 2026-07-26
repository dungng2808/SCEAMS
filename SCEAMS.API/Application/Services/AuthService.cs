using SCEAMS.Application.Common;
using SCEAMS.Application.DTOs;
using SCEAMS.Application.Interfaces;
using SCEAMS.Domain.Entities;
using SCEAMS.Domain.Enums;

namespace SCEAMS.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IAccessTokenService _accessTokenService;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IAccessTokenService accessTokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _accessTokenService = accessTokenService;
    }

    public async Task<Result<RegisteredStudentResponseDto>>
        RegisterStudentAsync(
            RegisterStudentRequestDto request,
            CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var studentCode = request.StudentCode.Trim().ToUpperInvariant();

        if (await _unitOfWork.Users.EmailExistsAsync(
                email,
                cancellationToken))
        {
            return Result<RegisteredStudentResponseDto>.Fail(
                "Email is already registered.",
                StatusCodes.Status409Conflict);
        }

        if (await _unitOfWork.Users.StudentCodeExistsAsync(
                studentCode,
                cancellationToken))
        {
            return Result<RegisteredStudentResponseDto>.Fail(
                "StudentCode is already registered.",
                StatusCodes.Status409Conflict);
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            StudentCode = studentCode,
            PhoneNumber = NormalizeOptionalValue(request.PhoneNumber),
            Role = UserRole.Student,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordService.HashPassword(
            user,
            request.Password);

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new RegisteredStudentResponseDto(
            Id: user.Id,
            FullName: user.FullName,
            Email: user.Email,
            StudentCode: user.StudentCode,
            PhoneNumber: user.PhoneNumber,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt);

        return Result<RegisteredStudentResponseDto>.Created(
            response,
            "Student account registered successfully.");
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null ||
            !_passwordService.VerifyPassword(
                user,
                user.PasswordHash,
                request.Password))
        {
            return Result<LoginResponseDto>.Fail(
                "Email or password is incorrect.",
                StatusCodes.Status401Unauthorized);
        }

        if (!user.IsActive)
        {
            return Result<LoginResponseDto>.Fail(
                "Account is inactive. Contact an administrator.",
                StatusCodes.Status403Forbidden);
        }

        var token = _accessTokenService.Create(user);
        var response = new LoginResponseDto(
            AccessToken: token.Value,
            TokenType: "Bearer",
            ExpiresAtUtc: token.ExpiresAtUtc,
            User: new AuthenticatedUserDto(
                Id: user.Id,
                FullName: user.FullName,
                Email: user.Email,
                StudentCode: user.StudentCode,
                Role: user.Role.ToString()));

        return Result<LoginResponseDto>.Ok(
            response,
            "Login successful.");
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
