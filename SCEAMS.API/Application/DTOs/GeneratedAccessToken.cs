namespace SCEAMS.Application.DTOs;

public sealed record GeneratedAccessToken(
    string Value,
    DateTime ExpiresAtUtc);
