namespace SCEAMS.Application.DTOs;

public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    AuthenticatedUserDto User);

public sealed record AuthenticatedUserDto(
    int Id,
    string FullName,
    string Email,
    string? StudentCode,
    string Role);
