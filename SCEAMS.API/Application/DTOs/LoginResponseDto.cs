namespace SCEAMS.Application.DTOs;

public sealed record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    AuthenticatedUserDto User);

public sealed record AuthenticatedUserDto(
    int Id,
    string FullName,
    string Email,
    string? StudentCode,
    string Role);
