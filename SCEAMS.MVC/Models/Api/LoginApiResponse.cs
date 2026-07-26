namespace SCEAMS.MVC.Models.Api;

public sealed record LoginApiResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    AuthenticatedUserApiResponse User);

public sealed record AuthenticatedUserApiResponse(
    int Id,
    string FullName,
    string Email,
    string? StudentCode,
    string Role);
