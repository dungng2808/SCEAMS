namespace SCEAMS.Application.DTOs;

public sealed record UserActiveStatusResponseDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive);
