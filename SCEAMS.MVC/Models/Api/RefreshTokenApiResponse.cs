namespace SCEAMS.MVC.Models.Api;

public sealed record RefreshTokenApiResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc);
