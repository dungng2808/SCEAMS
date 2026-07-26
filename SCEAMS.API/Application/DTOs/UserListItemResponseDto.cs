namespace SCEAMS.Application.DTOs;

public sealed record UserListItemResponseDto(
    int Id,
    string FullName,
    string Email,
    string? StudentCode,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTime CreatedAt);
