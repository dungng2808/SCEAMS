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
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IAccessTokenService accessTokenService,
        IRefreshTokenService refreshTokenService,
        TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _accessTokenService = accessTokenService;
        _refreshTokenService = refreshTokenService;
        _timeProvider = timeProvider;
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

        var accessToken = _accessTokenService.Create(user);
        var refreshToken = _refreshTokenService.Create();

        user.RefreshTokenHash = refreshToken.Hash;
        user.RefreshTokenExpiresAt = refreshToken.ExpiresAtUtc;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new LoginResponseDto(
            AccessToken: accessToken.Value,
            RefreshToken: refreshToken.Value,
            TokenType: "Bearer",
            ExpiresAtUtc: accessToken.ExpiresAtUtc,
            RefreshTokenExpiresAtUtc:
                refreshToken.ExpiresAtUtc,
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

    public async Task<Result<RefreshTokenResponseDto>> RefreshAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var suppliedRefreshToken = request.RefreshToken.Trim();
        var currentRefreshTokenHash =
            _refreshTokenService.ComputeHash(
                suppliedRefreshToken);
        var user = await _unitOfWork.Users
            .GetByRefreshTokenHashAsync(
                currentRefreshTokenHash,
                cancellationToken);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        if (user is null ||
            !user.IsActive ||
            user.RefreshTokenExpiresAt is null ||
            user.RefreshTokenExpiresAt <= utcNow)
        {
            return InvalidRefreshToken();
        }

        var replacementRefreshToken =
            _refreshTokenService.Create();
        var accessToken = _accessTokenService.Create(user);
        var rotated = await _unitOfWork.Users
            .TryRotateRefreshTokenAsync(
                user.Id,
                currentRefreshTokenHash,
                replacementRefreshToken.Hash,
                replacementRefreshToken.ExpiresAtUtc,
                utcNow,
                cancellationToken);

        if (!rotated)
        {
            return InvalidRefreshToken();
        }

        return Result<RefreshTokenResponseDto>.Ok(
            new RefreshTokenResponseDto(
                AccessToken: accessToken.Value,
                RefreshToken:
                    replacementRefreshToken.Value,
                TokenType: "Bearer",
                ExpiresAtUtc: accessToken.ExpiresAtUtc,
                RefreshTokenExpiresAtUtc:
                    replacementRefreshToken
                        .ExpiresAtUtc),
            "Token refreshed successfully.");
    }

    public async Task<Result> RevokeAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var refreshTokenHash =
            _refreshTokenService.ComputeHash(
                request.RefreshToken.Trim());

        await _unitOfWork.Users.RevokeRefreshTokenAsync(
            refreshTokenHash,
            cancellationToken);

        return Result.NoContent(
            "Refresh token revoked.");
    }

    private static Result<RefreshTokenResponseDto>
        InvalidRefreshToken()
    {
        return Result<RefreshTokenResponseDto>.Fail(
            "Refresh token is invalid, expired, or already used.",
            StatusCodes.Status401Unauthorized);
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
