namespace SCEAMS.Application.DTOs;

public sealed record UserRoleResponseDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive);
