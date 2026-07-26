namespace SCEAMS.Application.DTOs;

public sealed record GeneratedRefreshToken(
    string Value,
    string Hash,
    DateTime ExpiresAtUtc);
