namespace SCEAMS.Application.DTOs;

public sealed record CreatedUserResponseDto(
    int Id,
    string FullName,
    string Email,
    string? StudentCode,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTime CreatedAt);
