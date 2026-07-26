namespace SCEAMS.Application.DTOs;

public sealed record ClubCategoryResponseDto(
    int Id,
    string Name,
    string? Description);
