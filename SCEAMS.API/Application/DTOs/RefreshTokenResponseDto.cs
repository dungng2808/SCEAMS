namespace SCEAMS.Application.DTOs;

public sealed record RefreshTokenResponseDto(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc);
